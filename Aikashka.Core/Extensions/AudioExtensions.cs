#region

using Discord.Audio;
using NAudio.Wave;
using Serilog;

#endregion

namespace Aikashka.Core.Extensions;

public static class AudioExtensions
{
    public static async Task PlayAudio(this IAudioClient audioClient, Stream audio,
                                       CancellationToken cancellationToken = default)
    {
        Log.Information("Playing audio");

        await using var stream = audioClient.CreatePCMStream(AudioApplication.Voice, bufferMillis: 200);
        await using var reader = new WaveFileReader(audio);

        var outFormat = new WaveFormat(48000, 16, 2);
        var converter = new WaveFormatConversionStream(outFormat, reader);

        // add silence because Discord.Net is a shit sometimes
        var generator = new SilenceProvider(outFormat).ToSampleProvider();
        var silence = generator.Take(TimeSpan.FromSeconds(1));

        if (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException();
        }

        var ms = new MemoryStream();
        WaveFileWriter.WriteWavFileToStream(ms, silence.FollowedBy(converter.ToSampleProvider()).ToWaveProvider16());

        ms.Position = 0;

        await ms.CopyToAsync(stream, cancellationToken);
        await stream.FlushAsync(cancellationToken);

        Log.Information("Playback finished");
    }

    public static Task PlaySilence(this IAudioClient audioClient, int timeMs)
    {
        var outFormat = new WaveFormat(48000, 16, 2);

        var generator = new SilenceProvider(outFormat).ToSampleProvider();
        var silence = generator.Take(TimeSpan.FromMilliseconds(timeMs));

        var ms = new MemoryStream();
        WaveFileWriter.WriteWavFileToStream(ms, silence.ToWaveProvider16());

        ms.Position = 0;

        return audioClient.PlayAudio(ms);
    }
}
