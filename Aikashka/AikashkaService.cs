#region

using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

#endregion

namespace Aikashka;

public sealed class AikashkaService : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly DiscordSocketClient _client;
    private readonly InteractionHandler _interactionHandler;

    public AikashkaService(IConfiguration configuration, DiscordSocketClient client,
                           InteractionHandler interactionHandler)
    {
        _configuration = configuration;
        _client = client;
        _interactionHandler = interactionHandler;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _interactionHandler.InitializeAsync();

        await _client.LoginAsync(TokenType.Bot, _configuration["Bot:Token"]);
        await _client.StartAsync();

        await Task.Delay(-1, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.StopAsync();
    }
}
