#region

using Aikashka.Core.Data;
using Discord.Audio;
using Discord.WebSocket;

#endregion

namespace Aikashka.Core.Audio;

public sealed class AudioProcessor : IDisposable
{
    private static readonly HttpClient HttpClient = new();

    private readonly CancellationTokenSource _cts;

    private readonly SocketGuildUser _member;
    private readonly AudioStreamWrapper _wrapper;

    public event EventHandler<AudioProcessedArgs>? AudioProcessed;
    public ulong MemberId => _member.Id;
    public string MemberUsername => _member.Username;

    public AudioProcessor(SocketGuildUser member, AudioInStream stream)
    {
        _cts = new CancellationTokenSource();
        _member = member;
        _wrapper = new AudioStreamWrapper(stream);
    }

    public async Task BackgroundProcess()
    {
        while (!_cts.IsCancellationRequested)
        {
            var data = await _wrapper.ReadUntilMuted(cancellationToken: _cts.Token);

            if (_cts.IsCancellationRequested)
            {
                return;
            }

            var form = new MultipartFormDataContent();
            var content = new StreamContent(data);
            form.Add(content, "audio", "audio");
            var res = await HttpClient.PostAsync("http://127.0.0.1:5000/process_audio", form, _cts.Token);
            var text = await res.Content.ReadAsStringAsync(_cts.Token);

            switch (text)
            {
                case "[Aikashka] 0":
                    break;
                case "[Aikashka] -1":
                    AudioProcessed?.Invoke(this, new AudioProcessedArgs(_member, Languages.PromptUnableToHear));
                    break;
                default:
                    AudioProcessed?.Invoke(this, new AudioProcessedArgs(_member, text));
                    break;
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
    }
}
