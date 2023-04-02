#region

using Discord.Audio;
using NAudio.Utils;
using NAudio.Wave;
using Serilog;

#endregion

namespace Aikashka.Core.Audio;

public sealed class AudioStreamWrapper
{
    private readonly AudioInStream _stream;

    public AudioStreamWrapper(AudioInStream stream)
    {
        _stream = stream;
    }

    public async Task<MemoryStream> ReadUntilMuted(int timeoutMs = 500, CancellationToken cancellationToken = default)
    {
        try
        {
            var ms = new MemoryStream();

            var waveWriter =
                new WaveFileWriter(new IgnoreDisposeStream(ms), new WaveFormat(48000, 2));

            var buffer = new byte[3840];
            var isEmpty = true;
            while (true)
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,
                                                                          new CancellationTokenSource(timeoutMs).Token);

                int read;
                try
                {
                    read = await _stream.ReadAsync(buffer, cts.Token);

                    isEmpty = false;
                }
                catch (OperationCanceledException)
                {
                    if (!isEmpty)
                    {
                        break;
                    }

                    continue;
                }

                await waveWriter.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }

            await waveWriter.FlushAsync(cancellationToken);
            await waveWriter.DisposeAsync();

            ms.Position = 0;
            return ms;
        }
        catch (Exception e)
        {
            Log.Error(e, "zz bruh");
            return null;
        }
    }
}
