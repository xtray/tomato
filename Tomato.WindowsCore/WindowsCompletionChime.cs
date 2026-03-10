using System.Text;

namespace Tomato.WindowsCore;

public static class WindowsCompletionChime
{
    private const int SampleRate = 44_100;

    private readonly record struct ChimeNote(double Frequency, double DurationSeconds);

    public static byte[] CreateWaveData(WindowsTimerCompletionAudioEvent audioEvent, double volume)
    {
        if (audioEvent == WindowsTimerCompletionAudioEvent.None)
        {
            return [];
        }

        var normalizedVolume = NormalizeVolume(volume);
        if (normalizedVolume <= 0D)
        {
            return [];
        }

        var notes = Melody(audioEvent);
        var pcmData = RenderPcm(notes, normalizedVolume);
        return CreateWaveContainer(pcmData);
    }

    private static ChimeNote[] Melody(WindowsTimerCompletionAudioEvent audioEvent)
    {
        return audioEvent switch
        {
            WindowsTimerCompletionAudioEvent.WorkCompleted =>
            [
                new ChimeNote(659.25, 0.24),
                new ChimeNote(783.99, 0.24),
                new ChimeNote(987.77, 0.38),
                new ChimeNote(0, 0.10),
                new ChimeNote(783.99, 0.24),
                new ChimeNote(1046.50, 0.46),
                new ChimeNote(1318.51, 0.62),
                new ChimeNote(0, 0.12),
                new ChimeNote(987.77, 0.34),
                new ChimeNote(1174.66, 0.38),
                new ChimeNote(1318.51, 0.56)
            ],
            WindowsTimerCompletionAudioEvent.BreakCompleted =>
            [
                new ChimeNote(523.25, 0.22),
                new ChimeNote(659.25, 0.22),
                new ChimeNote(783.99, 0.30),
                new ChimeNote(0, 0.08),
                new ChimeNote(880.00, 0.26),
                new ChimeNote(1046.50, 0.34),
                new ChimeNote(1174.66, 0.34),
                new ChimeNote(0, 0.10),
                new ChimeNote(987.77, 0.28),
                new ChimeNote(1318.51, 0.42),
                new ChimeNote(1567.98, 0.56)
            ],
            _ => []
        };
    }

    private static byte[] RenderPcm(ChimeNote[] notes, double volume)
    {
        var samples = new List<short>();
        var amplitude = short.CreateSaturating(short.MaxValue * volume * 0.35D);

        foreach (var note in notes)
        {
            var sampleCount = Math.Max((int)(note.DurationSeconds * SampleRate), 1);
            for (var i = 0; i < sampleCount; i++)
            {
                short sample;
                if (note.Frequency <= 0D)
                {
                    sample = 0;
                }
                else
                {
                    var time = (double)i / SampleRate;
                    var angle = 2D * Math.PI * note.Frequency * time;
                    var harmonic = Math.Sin(angle) + (0.35D * Math.Sin(angle * 2D));
                    sample = short.CreateSaturating(amplitude * Envelope(i, sampleCount) * harmonic);
                }

                samples.Add(sample);
            }
        }

        var pcm = new byte[samples.Count * sizeof(short)];
        Buffer.BlockCopy(samples.ToArray(), 0, pcm, 0, pcm.Length);
        return pcm;
    }

    private static double Envelope(int sampleIndex, int totalSamples)
    {
        var attackSamples = Math.Max((int)(SampleRate * 0.012D), 1);
        var releaseSamples = Math.Max((int)(SampleRate * 0.060D), 1);

        if (sampleIndex < attackSamples)
        {
            return (double)sampleIndex / attackSamples;
        }

        if (sampleIndex >= totalSamples - releaseSamples)
        {
            var remaining = Math.Max(totalSamples - sampleIndex, 0);
            return (double)remaining / releaseSamples;
        }

        return 1D;
    }

    private static byte[] CreateWaveContainer(byte[] pcmData)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);

        const ushort audioFormat = 1;
        const ushort channelCount = 1;
        const ushort bitsPerSample = 16;
        var byteRate = SampleRate * channelCount * (bitsPerSample / 8);
        var blockAlign = (ushort)(channelCount * (bitsPerSample / 8));

        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + pcmData.Length);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write(audioFormat);
        writer.Write(channelCount);
        writer.Write(SampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(pcmData.Length);
        writer.Write(pcmData);
        writer.Flush();
        return stream.ToArray();
    }

    private static double NormalizeVolume(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return WindowsAppState.DefaultCompletionChimeVolume;
        }

        return Math.Clamp(value, WindowsAppState.MinCompletionChimeVolume, WindowsAppState.MaxCompletionChimeVolume);
    }
}
