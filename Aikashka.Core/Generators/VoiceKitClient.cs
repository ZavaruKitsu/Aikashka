#region

using Grpc.Core;
using NAudio.Utils;
using NAudio.Wave;
using Tinkoff.Cloud.Tts.V1;
using Tinkoff.VoiceKit;

#endregion

namespace Aikashka.Core.Generators;

public sealed class VoiceKitClient
{
    private readonly string _voice;
    private readonly TextToSpeech.TextToSpeechClient _clientTts;
    private readonly Auth _authTts;

    public VoiceKitClient(string apiKey, string secretKey, string voice)
    {
        _voice = voice;
        _authTts = new Auth(apiKey, secretKey, "tinkoff.cloud.tts");

        var cred = new SslCredentials();
        var channelTts = new Channel("api.tinkoff.ai:443", cred);

        _clientTts = new TextToSpeech.TextToSpeechClient(channelTts);
    }

    private Metadata GetMetadataTts()
    {
        var header = new Metadata { { "Authorization", $"Bearer {_authTts.Token}" } };
        return header;
    }

    public async Task<MemoryStream> StreamingSynthesize(string synthesizeInput,
                                                        CancellationToken cancellationToken = default)
    {
        var request = new SynthesizeSpeechRequest
        {
            AudioConfig = new AudioConfig
            {
                AudioEncoding = AudioEncoding.Linear16,
                SampleRateHertz = 48000
            },
            Input = new SynthesisInput
            {
                Text = synthesizeInput
            },
            Voice = new VoiceSelectionParams
            {
                Name = _voice,
                SsmlGender = SsmlVoiceGender.Female
            }
        };

        var stream = _clientTts.StreamingSynthesize(request, GetMetadataTts(), cancellationToken: cancellationToken);

        var audioBuffer = new List<byte[]>();
        while (await stream.ResponseStream.MoveNext())
        {
            audioBuffer.Add(stream.ResponseStream.Current.AudioChunk.ToByteArray());
        }

        var ms = new MemoryStream();
        var audioBytes = audioBuffer.SelectMany(byteArr => byteArr).ToArray();
        await using (var waveFileWriter =
                     new WaveFileWriter(new IgnoreDisposeStream(ms), new WaveFormat(48000, 1)))
        {
            await waveFileWriter.WriteAsync(audioBytes, cancellationToken);
        }

        ms.Position = 0;

        return ms;
    }
}
