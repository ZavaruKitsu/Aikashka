#region

using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Serilog;
using Serilog.Events;

#endregion

namespace Aikashka;

public sealed class InteractionHandler
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _handler;
    private readonly IServiceProvider _services;

    public InteractionHandler(DiscordSocketClient client, InteractionService handler, IServiceProvider services)
    {
        _client = client;
        _handler = handler;
        _services = services;
    }

    public async Task InitializeAsync()
    {
        // Process when the client is ready, so we can register our commands.
        _client.Ready += ReadyAsync;
        _client.Log += LogAsync;
        _handler.Log += LogAsync;

        // Add the public modules that inherit InteractionModuleBase<T> to the InteractionService
        await _handler.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

        // Process the InteractionCreated payloads to execute Interactions commands
        _client.InteractionCreated += HandleInteraction;
    }

    private Task LogAsync(LogMessage message)
    {
        var severity = message.Severity switch
                       {
                           LogSeverity.Critical => LogEventLevel.Fatal,
                           LogSeverity.Error    => LogEventLevel.Error,
                           LogSeverity.Warning  => LogEventLevel.Warning,
                           LogSeverity.Info     => LogEventLevel.Information,
                           LogSeverity.Verbose  => LogEventLevel.Verbose,
                           LogSeverity.Debug    => LogEventLevel.Debug,
                           _                    => LogEventLevel.Information
                       };
        Log.Write(severity, message.Exception, "[{Source}] {Message}", message.Source, message.Message);

        return Task.CompletedTask;
    }

    private async Task ReadyAsync()
    {
        //        await _handler.RegisterCommandsToGuildAsync(484025467134017568);
        //        await _handler.RegisterCommandsToGuildAsync(1090753606807998467);
    }

    private async Task HandleInteraction(SocketInteraction interaction)
    {
        try
        {
            // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules.
            var context = new SocketInteractionContext(_client, interaction);

            // Execute the incoming command.
            var result = await _handler.ExecuteCommandAsync(context, _services);

            if (!result.IsSuccess)
            {
                switch (result.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        // implement
                        break;
                }
            }
        }
        catch
        {
            // If Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
            // response, or at least let the user know that something went wrong during the command execution.
            if (interaction.Type is InteractionType.ApplicationCommand)
            {
                await interaction
                      .GetOriginalResponseAsync()
                      .ContinueWith(async msg => await msg.Result.DeleteAsync());
            }
        }
    }
}
