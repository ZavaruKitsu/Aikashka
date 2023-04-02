#region

using Aikashka.Core;
using Aikashka.Core.Audio;
using Aikashka.Core.Extensions;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Serilog;

#endregion

namespace Aikashka.Modules;

public class AudioModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly RuntimeStorage _runtimeStorage;
    private readonly IServiceProvider _services;

    public AudioModule(RuntimeStorage runtimeStorage, IServiceProvider services)
    {
        _runtimeStorage = runtimeStorage;
        _services = services;
    }

    [SlashCommand("go", "Go!", runMode: RunMode.Async)]
    public async Task RunAikashka()
    {
        await RespondAsync("Let's talk!");

        var channel = (Context.User as IGuildUser)?.VoiceChannel!;
        var audioClient = await channel.ConnectAsync();

        var room = _runtimeStorage.Rooms.GetOrAdd(Context.Guild.Id, _ =>
        {
            var roomContext = new RoomContext(
                                              audioClient,
                                              Context.Guild,
                                              (SocketTextChannel)Context.Channel,
                                              channel,
                                              _services
                                             );
            roomContext.BackgroundGenerator().Forget();

            return roomContext;
        });
        room.ClearAudioProcessors();

        audioClient.StreamCreated += async (userId, stream) =>
        {
            var member = Context.Guild.GetUser(userId);
            if (member == null)
            {
                // well it happens sometimes...
                await Context.Guild.DownloadUsersAsync();

                member = Context.Guild.GetUser(userId);
            }

            var processor = new AudioProcessor(member, stream);

            room.AddAudioProcessor(processor);
        };
        audioClient.StreamDestroyed += async id =>
        {
            room.RemoveAudioProcessor(id);

            if (room.UsersCount == 0)
            {
                await room.Destroy();
            }
        };

        await audioClient.PlaySilence(100);

        foreach (var (userId, userStream) in audioClient.GetStreams())
        {
            var member = Context.Guild.GetUser(userId);
            var processor = new AudioProcessor(member, userStream);

            room.AddAudioProcessor(processor);
        }
    }

    [SlashCommand("cum", "Cummunicate.", runMode: RunMode.Async)]
    public async Task AddMessage(string text)
    {
        var room = _runtimeStorage.Rooms[Context.Guild.Id];
        room.History.AddMessage(Context.Guild.GetUser(Context.User.Id), text);

        await RespondAsync("OK, noticed!");
    }

    [SlashCommand("state", "State of this useless bot.", runMode: RunMode.Async)]
    public async Task State()
    {
        var room = _runtimeStorage.Rooms[Context.Guild.Id];
        var history = room.History.GetHistoryFormatted();

        Log.Warning(history);

        await RespondAsync(history);
    }

    [SlashCommand("clear", "Clear all history.", runMode: RunMode.Async)]
    public async Task Clear()
    {
        var room = _runtimeStorage.Rooms[Context.Guild.Id];

        room.History.Clear((Context.User as SocketGuildUser)!);

        await RespondAsync("*flash*");
    }
}
