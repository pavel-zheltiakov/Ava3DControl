using AvaMediaPlayer;

namespace Ava3D.Demo.Story;

/// <summary>
/// The one place in this demo that knows what plays the sound.
///
/// Everything else — <see cref="Tone"/>, <see cref="Cues"/>, <see cref="Soundtrack"/> — deals in
/// <c>float[]</c> and seconds and never names <c>AvaMediaPlayer</c>. That is worth a wrapper this thin for
/// two reasons, and only the second is about tidiness.
///
/// The first is that this repository builds the same demo twice. Here it has the player's source next to
/// it; in the generated public repository there is no <c>src</c> directory at all, and the demo restores
/// the control from nuget.org the way anybody else's project would. The player is not published, so over
/// there this file is swapped for <c>Speaker.Silent.cs</c> — same type, same members, no sound — and every
/// other file in the folder compiles unchanged. The public demo is the film without a soundtrack rather
/// than a demo that does not build.
///
/// The second is the rule the player is kept under: nothing of Ava3D's may appear in it, because it gets
/// lifted out of this repository later and one <c>using Ava3D;</c> turns that into a rewrite. A boundary
/// only holds if it is somewhere you can see, and this file is where it is.
/// </summary>
internal sealed class Speaker : IDisposable
{
    private readonly AudioPlayer _player;
    private readonly Tape? _tape;

    private Speaker(AudioPlayer player, Tape? tape = null)
    {
        _player = player;
        _tape = tape;
    }

    /// <summary>Opens the machine's audio, or returns null when there is not any — see
    /// <see cref="AudioPlayer.Open()"/> for why that is an ordinary answer rather than a failure.</summary>
    public static Speaker? Open()
    {
        // The host's own device first, if it left one. See HostAudio.
        var player = HostAudio.Supply?.Invoke() is { } device ? AudioPlayer.Open(device) : AudioPlayer.Open();

        return player == null ? null : new Speaker(player);
    }

    /// <summary>
    /// Opens onto a buffer rather than a speaker: nothing is heard, and <see cref="Record"/> renders time
    /// on demand as fast as the machine can do the arithmetic.
    ///
    /// This is the whole reason <see cref="AudioDevice"/> is an abstraction rather than a hard-coded
    /// audio queue. A device is asked for the next block of samples and does whatever it likes with them,
    /// so "write them to a file" is a device, and the film's soundtrack can be rendered on a build server
    /// with no sound card at three hundred times real speed. Nothing above this line knows the difference.
    /// </summary>
    public static Speaker? Recording(int sampleRate)
    {
        var tape = new Tape(sampleRate);
        var player = AudioPlayer.Open(tape);

        return player == null ? null : new Speaker(player, tape);
    }

    /// <summary>Renders <paramref name="seconds"/> of whatever is playing onto the tape. Does nothing on a
    /// speaker that is actually a speaker.</summary>
    public void Record(float seconds) => _tape?.Render((int)(seconds * SampleRate));

    /// <summary>Writes the tape to a .wav file.</summary>
    public void Save(string path)
    {
        if (_tape != null)
            Wav.Write(path, _tape.Written, SampleRate);
    }

    /// <summary>Samples a second. Cues are generated at this rate so nothing has to be resampled.</summary>
    public int SampleRate => _player.SampleRate;

    /// <summary>Where the sound is going and at what rate, for the line the demo prints about itself.</summary>
    public string Describe() => $"{_player.DeviceName} at {SampleRate} Hz";

    /// <summary>Silence, without stopping the film's sound — so unmuting lands where the film is.</summary>
    public bool Muted
    {
        get => _player.Muted;
        set => _player.Muted = value;
    }

    /// <summary>Starts <paramref name="samples"/> and hands back the voice playing it.</summary>
    public Voice Play(float[] samples, float volume = 1f, bool loop = false) =>
        new(_player.Play(new AudioClip(samples, SampleRate), volume, loop));

    /// <summary>Everything off at once. What leaving the film wants.</summary>
    public void StopAll() => _player.StopAll();

    public void Dispose() => _player.Dispose();

    /// <summary>
    /// A device that is not a device: it renders only when asked and keeps what it rendered.
    ///
    /// It never calls back on a thread of its own, which is what makes an offline render deterministic —
    /// the caller alternates "move the film on a frame" and "give me a frame's worth of audio", so the
    /// score is sampled at exactly sixty hertz with no scheduler in it. A real device rendering the same
    /// film is at the mercy of when its thread happens to wake.
    /// </summary>
    private sealed class Tape(int sampleRate) : AudioDevice
    {
        private AudioFill? _fill;
        private float[] _block = [];

        public List<float> Written { get; } = [];

        public override int SampleRate => sampleRate;

        public override void Start(AudioFill fill) => _fill = fill;

        public override void Dispose() { }

        public void Render(int frames)
        {
            if (frames <= 0)
                return;

            if (_block.Length < frames)
                _block = new float[frames];

            var span = _block.AsSpan(0, frames);
            _fill?.Invoke(span);

            Written.AddRange(span);
        }
    }
}

/// <summary>
/// Where a head that has audio this process cannot open by itself leaves it.
///
/// One platform needs this and it is the browser. A tab's sound arrives through JavaScript interop that
/// only a project targeting the browser can compile, so the device cannot live in the player or in this
/// shared assembly — it lives in <c>Ava3D.Demo.Browser</c>, which puts a factory here before Avalonia
/// starts, and everything above this line goes on knowing nothing about any of it.
///
/// A factory rather than a device, because the film opens a speaker every time its sound switch is turned
/// on and closes it every time the scene is replaced, and a disposed device cannot be started again.
///
/// Public, and in this file, for the same reason the rest of it is: this is the file that names
/// <c>AvaMediaPlayer</c>, and <see cref="AudioDevice"/> in the signature is exactly why. The public copy
/// of the demo compiles <c>Speaker.Silent.cs</c> instead and does not have this type at all — nor does it
/// have the browser file that would look for it.
/// </summary>
public static class HostAudio
{
    /// <summary>Makes a device, or returns null if the host turned out not to have one after all.</summary>
    public static Func<AudioDevice?>? Supply { get; set; }
}

/// <summary>One sound, playing: how loud, and a way to stop it. See <see cref="Speaker"/> for why the
/// demo has its own name for this.</summary>
internal sealed class Voice(AudioVoice inner)
{
    /// <summary>How loud, 0 to 1. Assigning it every frame is the intended use — the player glides to it
    /// rather than stepping, so a fade is a number written sixty times a second and nothing else.</summary>
    public float Volume
    {
        get => inner.Volume;
        set => inner.Volume = value;
    }

    public void Stop() => inner.Stop();
}
