#region

using System.Text;
using Aikashka.Core;
using Aikashka.Core.Generators;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

#endregion

namespace Aikashka;

public static class HostingExtensions
{
    public static ILoggingBuilder AddAikashkaLogging(this ILoggingBuilder loggingBuilder)
    {
        // russian lang support in console
        Console.OutputEncoding = Encoding.UTF8;

        Log.Logger = new LoggerConfiguration()
                     .MinimumLevel.Verbose()
                     .Enrich.FromLogContext()
                     .WriteTo.Console()
                     .CreateLogger();

        // I hate ILogger<..>, so no.

        return loggingBuilder;
    }

    public static IServiceCollection AddAikashkaServices(this IServiceCollection services)
    {
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged |
                             GatewayIntents.MessageContent |
                             GatewayIntents.GuildMembers
        };

        var client = new DiscordSocketClient(config);

        services
            .AddSingleton(client)
            .AddSingleton<InteractionService>()
            .AddSingleton<InteractionHandler>()
            .AddSingleton<RuntimeStorage>()
            .AddSingleton<TextGenerator>()
            .AddSingleton<VoiceGenerator>();

        services
            .AddHostedService<AikashkaService>();

        return services;
    }
}
