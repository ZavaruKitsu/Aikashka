#region

using System.Text;
using System.Text.RegularExpressions;
using Aikashka.Core.Audio;
using Aikashka.Core.Data;
using Aikashka.Core.Extensions;
using Aikashka.Core.Generators;
using Aikashka.Core.Text;
using Aikashka.Core.Utilities;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

#endregion

namespace Aikashka.Core;

public sealed partial class RoomContext
{
    private static readonly List<string> ShutUpWords = Languages.ShutUpWords
                                                                .Split(";",
                                                                       StringSplitOptions.TrimEntries |
                                                                       StringSplitOptions.RemoveEmptyEntries)
                                                                .ToList();

    private readonly List<AudioProcessor> _audioProcessors = new();

    private readonly IAudioClient _audioClient;
    private readonly SocketGuild _guild;
    private readonly SocketTextChannel _textChannel;
    private readonly IVoiceChannel _voiceChannel;

    private readonly RuntimeStorage _runtimeStorage;
    private readonly TextGenerator _textGenerator;
    private readonly VoiceGenerator _voiceGenerator;

    private readonly CancellationTokenSource _cts;
    private CancellationTokenSource _voiceCts;

    public RoomHistory History { get; }
    public DateTime LastSpoke { get; set; }
    public int UsersCount => _audioProcessors.Count;

    public RoomContext(IAudioClient audioClient, SocketGuild guild, SocketTextChannel textChannel,
                       IVoiceChannel voiceChannel, IServiceProvider services)
    {
        History = new RoomHistory();

        _audioClient = audioClient;
        _guild = guild;
        _textChannel = textChannel;
        _voiceChannel = voiceChannel;

        _runtimeStorage = services.GetRequiredService<RuntimeStorage>();
        _textGenerator = services.GetRequiredService<TextGenerator>();
        _voiceGenerator = services.GetRequiredService<VoiceGenerator>();

        _cts = new CancellationTokenSource();
        _voiceCts = new CancellationTokenSource();
    }

    public void AddAudioProcessor(AudioProcessor audioProcessor)
    {
        Log.Information("Starting processor on {Id}", audioProcessor.MemberId);

        audioProcessor.BackgroundProcess().Forget();
        audioProcessor.AudioProcessed += OnAudioProcessed;

        _audioProcessors.Add(audioProcessor);
    }

    public void RemoveAudioProcessor(AudioProcessor audioProcessor)
    {
        audioProcessor.Dispose();
        _audioProcessors.Remove(audioProcessor);

        Log.Information("Removed audio processor {Id}", audioProcessor.MemberId);
    }

    public void RemoveAudioProcessor(ulong id)
    {
        var audioProcessor = _audioProcessors.FirstOrDefault(x => x.MemberId == id);
        if (audioProcessor == null)
        {
            return;
        }

        RemoveAudioProcessor(audioProcessor);
    }

    public void ClearAudioProcessors()
    {
        foreach (var audioProcessor in _audioProcessors)
        {
            audioProcessor.Dispose();
        }

        _audioProcessors.Clear();

        Log.Information("Removed all audio processors");
    }

    private void OnAudioProcessed(object? sender, AudioProcessedArgs args)
    {
        Log.Information("New message from {Member}: {Message}", args.Member, args.Text);

        History.AddMessage(args.Member, args.Text);
        LastSpoke = DateTime.Now;

        if (ShutUpWords.Any(x => args.Text.Contains(x, StringComparison.InvariantCultureIgnoreCase)))
        {
            _voiceCts.Cancel(); // women hah *sips*
        }
    }

    public override string ToString()
    {
        return $"Room: {_guild}";
    }

    public async Task BackgroundGenerator()
    {
        var nextRandomSpeak = TimeSpan.FromSeconds(10);
        while (!_cts.IsCancellationRequested)
        {
            if (_voiceCts.IsCancellationRequested)
            {
                await Task.Delay(3000, _cts.Token);
                _voiceCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);

                continue;
            }

            if (LastSpoke == default || DateTime.Now - LastSpoke < nextRandomSpeak)
            {
                var lastMsg = History.LastMessage;

                if (string.IsNullOrWhiteSpace(lastMsg) || !RoomHistory.AikashkaNames.Any(x =>
                        lastMsg.Contains(x, StringComparison.InvariantCultureIgnoreCase)))
                {
                    await Task.Delay(50, _cts.Token);

                    continue;
                }
            }

            if (History.LastMessageFrom == RoomHistory.AikashkaName)
            {
                await Task.Delay(50, _cts.Token);

                continue;
            }

            nextRandomSpeak = TimeSpan.FromSeconds(Random.Shared.Next(5, 40));

            var preprompt = new StringBuilder(Languages.PromptChannelMembers);
            preprompt.Append(' ');
            preprompt.AppendJoin(", ",
                                 _audioProcessors.Select(x => NameService.GetMemberName(x.MemberId) ??
                                                              x.MemberUsername));
            preprompt.Append('.');

            var text = await _textGenerator.GenerateReply(History, preprompt.ToString(), _cts.Token);
            History.AddAikashkaMessage(text);

            Log.Information("Aikashka generated: {Text}", text);

            await ExecuteWills(text);
        }
    }

    private async Task ExecuteWills(string text)
    {
        var re = ActionRegex();
        var subs = re.Split(text).Skip(1).ToArray();

        var res = subs
                  .Where(x => x.StartsWith('['))
                  .Zip(subs.Where(y => !y.StartsWith('[')))
                  .Select(x => $"{x.First}{x.Second}");

        foreach (var will in res)
        {
            try
            {
                await ExecuteAikashkaWill(will);
            }
            catch (OperationCanceledException e)
            {
                Log.Warning(e, "Her will was canceled");
            }
            catch (Exception e)
            {
                Log.Error(e, "Error while executing her will");
            }
        }
    }

    private async Task ExecuteAikashkaWill(string text)
    {
        Log.Information("Executing will");

        if (text.StartsWith("[tts]"))
        {
            await TextToSpeech(text.Remove(0, 5));
        }
        else if (text.StartsWith("[msg]"))
        {
            await SendMessage(text.Remove(0, 5));
        }
        else if (text.StartsWith("[thought]"))
        {
            Log.Information("Her thoughts: {Text}", text.Remove(0, 9));
        }
        else if (text.StartsWith("[leave]"))
        {
            await LeaveChannel(text.Remove(0, 7));
        }
        else if (text.StartsWith("[kick "))
        {
            var match = KickRegex().Match(text);
            var group = match.Groups["user"];
            var user = group.Value;
            var restText = text[(group.Index + group.Length + 1)..].Trim();

            if (!string.IsNullOrWhiteSpace(restText))
            {
                await SendMessage(restText);
            }

            await KickUser(user);
        }
        else
        {
            Log.Error("AI generated wrong action: {Text}", text);
        }
    }

    private async Task KickUser(string user)
    {
        var id = NameService.GetMemberId(user);
        if (!id.HasValue)
        {
            Log.Warning("Aikashka tried to kick someone unknown...");

            // todo: fetch user from guild

            return;
        }

        var member = _guild.GetUser(id.Value);

        try
        {
            await member.ModifyAsync(x => x.Channel = null);
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to kick user");
        }
    }

    private async Task LeaveChannel(string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            await SendMessage(text);
        }

        await Destroy();
    }

    private async Task SendMessage(string text)
    {
        Log.Information("Sending message: {Text}", text);

        if (string.IsNullOrWhiteSpace(text))
        {
            text = Languages.EmptyResponseText;
        }

        var embedBuilder = new EmbedBuilder()
                           .WithAuthor(RoomHistory.AikashkaName)
                           .WithColor(Color.Purple)
                           .WithFields(
                                       new EmbedFieldBuilder()
                                           .WithName("Text")
                                           .WithValue(text.Trim())
                                      );

        await _textChannel.SendMessageAsync(embed: embedBuilder.Build());
    }

    private async Task TextToSpeech(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Log.Warning("She wanted to say smth, but the text was null");
            return;
        }

        text = text.Trim();
        text = ThoughtsRegex().Replace(text, "");

        _voiceCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);

        Log.Information("Generating voice");
        await using var audio = await _voiceGenerator.GenerateVoice(text, _voiceCts.Token);
        Log.Information("Voice generated");

        await _audioClient.PlayAudio(audio, _voiceCts.Token);
        LastSpoke = DateTime.Now;
    }

    public async Task Destroy()
    {
        _cts.Cancel();

        _runtimeStorage.Rooms.TryRemove(_guild.Id, out _);

        await Task.Delay(100);

        ClearAudioProcessors();

        try
        {
            await _voiceChannel.DisconnectAsync();
        }
        catch (Exception e)
        {
            Log.Warning(e, "Unable to leave the channel");
        }

        _audioClient.Dispose();
    }

    [GeneratedRegex(@"(\[.+?\])", RegexOptions.Singleline)]
    private static partial Regex ActionRegex();

    [GeneratedRegex(@"\[kick (?<user>.+)\]", RegexOptions.Singleline)]
    private static partial Regex KickRegex();

    [GeneratedRegex(@"\(.+?\)", RegexOptions.Singleline)]
    private static partial Regex ThoughtsRegex();
}
