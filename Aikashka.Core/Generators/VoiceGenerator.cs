#region

using Microsoft.Extensions.Configuration;

#endregion

namespace Aikashka.Core.Generators;

public sealed class VoiceGenerator
{
    private readonly VoiceKitClient _voiceKitClient;

    public VoiceGenerator(IConfiguration configuration)
    {
        _voiceKitClient = new VoiceKitClient(
                                             configuration["VoiceKit:ApiKey"],
                                             configuration["VoiceKit:SecretKey"],
                                             configuration["VoiceKit:Voice"]
                                            );
    }

    public async Task<Stream> GenerateVoice(string text, CancellationToken cancellationToken = default)
    {
        // todo: remove emojis & other unicode symbols
        var res = await _voiceKitClient.StreamingSynthesize(text, cancellationToken);
        return res;
    }
}
