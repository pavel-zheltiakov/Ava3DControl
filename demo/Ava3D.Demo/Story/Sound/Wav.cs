namespace Ava3D.Demo.Story;

/// <summary>
/// Writes samples to a .wav file, so the soundtrack can be listened to without running the demo.
///
/// Every other thing this film does can be reviewed from a screenshot. The sound cannot, which makes it
/// the one part of the demo where "it works on my machine" is the only report anybody can give — and that
/// is exactly the situation the rest of this repository has switches for. <c>AVA3D_CAPTURE</c> takes the
/// picture out of the running application so somebody else can look at it; this is the same idea pointed
/// at the other half.
///
/// Uncompressed 16-bit mono, which is the format every player on every platform opens without being asked
/// twice, and forty-four bytes of header that have not changed since 1991. There is no encoder here and
/// there should not be: an mp3 would be a dependency, a decision about quality, and a licence, to save a
/// file nobody keeps.
/// </summary>
internal static class Wav
{
    /// <summary>Writes <paramref name="samples"/> to <paramref name="path"/>, clamped and converted to
    /// signed 16-bit.</summary>
    public static void Write(string path, IReadOnlyList<float> samples, int sampleRate)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        using var file = File.Create(path);
        using var writer = new BinaryWriter(file);

        var bytes = samples.Count * sizeof(short);

        writer.Write("RIFF"u8);
        writer.Write(36 + bytes);
        writer.Write("WAVE"u8);

        writer.Write("fmt "u8);
        writer.Write(16);                       // the size of this chunk, for PCM
        writer.Write((short)1);                 // uncompressed
        writer.Write((short)1);                 // one channel
        writer.Write(sampleRate);
        writer.Write(sampleRate * sizeof(short)); // bytes a second
        writer.Write((short)sizeof(short));     // bytes a frame
        writer.Write((short)16);                // bits a sample

        writer.Write("data"u8);
        writer.Write(bytes);

        foreach (var sample in samples)
            writer.Write((short)(Math.Clamp(sample, -1f, 1f) * 32767f));
    }
}
