using System;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using Ava3D.Demo.Story;
using Avalonia.Threading;
using AvaMediaPlayer;

/// <summary>
/// The film's speaker in a browser tab.
///
/// <see cref="AudioDevice"/> exists for exactly this: the player has no P/Invoke here and could not reach
/// an audio API through one anyway, so the tab's own is supplied from the application side. It is this head
/// rather than the shared demo because <c>JSImport</c> only compiles under a browser target framework, and
/// it is not in the player because the player is a plain <c>net10.0</c> assembly that gets lifted out of
/// this repository later.
///
/// <b>Pushed rather than pulled.</b> Every other device in this demo is asked for the next few milliseconds
/// by something with a thread of its own. A tab has no such thread to offer, so this one runs the
/// relationship backwards: a timer on the UI thread takes blocks from the mixer and hands them to Web
/// Audio, which plays them back to back on the audio clock. A fifth of a second is kept scheduled ahead of
/// the speaker, which is what makes it survive its own thread — the software renderer can spend eighty
/// milliseconds on a frame without a gap being heard.
///
/// <b>It was half a second, and that turned out to be audible after all.</b> A one-shot is written wherever
/// the mixer has got to, which is by definition the far end of the schedule — so the deeper the schedule,
/// the later every cue arrives relative to the frame that triggered it. At half a second the lamps in
/// chapter 1 came on and were heard to come on a beat afterwards, which reads not as latency but as
/// somebody throwing the breakers in another room. The original note here said that for a film with a score
/// and footsteps the delay was inaudible. It said that about the beds, which have no attack in them, and it
/// was wrong about everything with a transient.
///
/// The headroom is bought back from the block size instead: half as long a block is twice as many crossings
/// a second, which costs a little and does not cost anything anybody can hear. What is left is about a
/// hundred and fifty milliseconds, which is inside what a picture and a sound are taken to be together.
/// Below that the answer is an AudioWorklet fed from a background thread, not a smaller number here.
/// </summary>
[SupportedOSPlatform("browser")]
internal sealed partial class BrowserAudio : AudioDevice
{
    /// <summary>
    /// Samples in a block, and therefore how much sound one interop call carries: 2048 is about 43 ms.
    ///
    /// Every block costs a call across the JavaScript boundary and an <c>AudioBuffer</c> the collector will
    /// have to take back, so this wants to be as large as the schedule can afford — and the schedule is what
    /// decides when a cue is heard, not the block size. It was twice this while the schedule was half a
    /// second deep, on the argument that nothing here is latency-critical. Nothing here is latency-critical
    /// and all of it is <i>sync</i>-critical, which is a different claim: the schedule cannot be shortened
    /// below about three blocks without a timer that misses two ticks being heard, so the block is what had
    /// to give. Twenty-three crossings a second instead of twelve.
    /// </summary>
    private const int Frames = 2048;

    /// <summary>
    /// How many blocks one tick may push, so a stall cannot turn into a flood.
    ///
    /// The pump fills until the schedule is deep enough, and the schedule is measured against a clock that
    /// keeps running while the page is frozen. A tab backgrounded for a minute comes back needing a minute
    /// of sound to catch up, and without a ceiling the first tick after it wakes tries to mix all sixty
    /// seconds of it in one go. With one, it pushes a second, finds itself still behind, and catches up over
    /// the next few ticks — audibly a stumble, rather than a page that stops responding.
    /// </summary>
    /// <remarks>Twenty-four rather than twelve since the block halved, so a tick still catches up about a
    /// second of sound and the sentence above stays true.</remarks>
    private const int BlocksPerTick = 24;

    /// <summary>
    /// Ticks before the schedule's depth is worth believing, and how often it is reported.
    ///
    /// The first few are the pump filling an empty schedule from nothing, which is shallow by definition
    /// and says nothing about whether it can keep up. Twenty ticks is a second; the report lands every
    /// twelve, the first of them beside the renderer's own — see MainView, and see there for why a browser
    /// head says what it did without being asked. It is the only way anybody finds out whether the sound
    /// kept up on a machine that is not this one, and a stutter nobody can measure is a bug report nobody
    /// can act on.
    ///
    /// <b>Every twelve seconds rather than once at twelve.</b> A single reading says whether the sound
    /// started; a series says whether anything is growing, and that is the question this head had no
    /// instrument for at all. The tab used to die at unpredictable moments with the sound on and never with
    /// it off, and the whole of the evidence was a trap with no message — see the heap size in the csproj.
    /// A line every twelve seconds costs a browser console nothing and is the difference between a crash
    /// that can be reasoned about and one that can only be reproduced.
    /// </summary>
    private const int Settle = 20, Report = 240;

    /// <summary>What the mixer fills. Reused, so a block costs no allocation on this side.</summary>
    private readonly float[] _mix = new float[Frames];

    /// <summary>
    /// The same block again as doubles, which is what actually crosses to JavaScript. Reused for the same
    /// reason. See <see cref="Push"/> for why there are two arrays instead of one view.
    /// </summary>
    private readonly double[] _block = new double[Frames];

    private readonly int _rate;

    private DispatcherTimer? _timer;
    private AudioFill? _fill;
    private bool _closed;

    /// <summary>Ticks so far, blocks pushed, and the shallowest the schedule ever got. See
    /// <see cref="Report"/>.</summary>
    private int _ticks, _pushed;
    private double _thinnest = double.MaxValue;

    private BrowserAudio(int rate) => _rate = rate;

    public override int SampleRate => _rate;

    /// <summary>
    /// Opens the tab's audio, or returns null when there is not any to open.
    ///
    /// Null is an ordinary answer here rather than a failure — a browser with audio disabled, a policy that
    /// forbids it, an engine with no <c>AudioContext</c> — and the demo treats it exactly as it treats a
    /// machine with no sound card: the film plays without a soundtrack.
    /// </summary>
    public static BrowserAudio? Open()
    {
        try
        {
            var rate = AudioOpen();
            return rate > 0 ? new BrowserAudio(rate) : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Starts the pump.
    ///
    /// On the dispatcher rather than a timer of its own, and that is a requirement rather than a
    /// convenience: an <c>AudioContext</c> belongs to the page's thread, and the pool thread a
    /// <see cref="System.Threading.Timer"/> would call back on has no way to reach it.
    /// </summary>
    public override void Start(AudioFill fill)
    {
        _fill = fill;

        // Faster than a block is long, so a tick that is late still leaves the schedule deep. The interval
        // is a floor and not a promise — this thread also draws the film — which is the reason the pump
        // works in seconds of scheduled sound rather than in blocks per tick.
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Default, (_, _) => Pump());
        _timer.Start();

        // Once immediately, so the first sound of the film starts on the switch being pressed rather than a
        // tick later.
        Pump();
    }

    /// <summary>
    /// Tops the schedule up to <c>audioTarget</c> seconds ahead of the speaker.
    ///
    /// Nothing escapes it. This is called from a timer on the UI thread, and an exception from a mixer or a
    /// missing browser API would otherwise arrive as an unhandled dispatcher exception — which in this
    /// application means the film stops being drawn because its sound went wrong.
    /// </summary>
    private void Pump()
    {
        if (_closed || _fill is null)
            return;

        try
        {
            var target = AudioTarget();
            var ahead = AudioAhead();
            if (_ticks > Settle)
                _thinnest = Math.Min(_thinnest, ahead);

            for (var i = 0; i < BlocksPerTick && ahead < target; i++)
            {
                _fill(_mix);

                // Widened by hand rather than reinterpreted, because what crosses the boundary is an array
                // of numbers and JavaScript has one numeric type. Four thousand conversions twelve times a
                // second is about fifty thousand a second, which is nothing next to the mixing that
                // produced them.
                for (var sample = 0; sample < Frames; sample++)
                    _block[sample] = _mix[sample];

                ahead = Push(_block, Frames);
                _pushed++;
            }

            if (++_ticks % Report == 0)
                Console.WriteLine(
                    $"[Ava3D.Demo] the tab's speaker has taken {_pushed} blocks "
                    + $"({_pushed * Frames / (float)_rate:0.0} s of audio) and never had less than "
                    + $"{_thinnest:0.000} s scheduled ahead of it; "
                    + $"managed heap {GC.GetTotalMemory(false) / 1_000_000f:0.0} MB.");
        }
        catch
        {
            // Whatever it was, it will be it again in fifty milliseconds. Stopping is the honest response:
            // silence is a state this demo already handles everywhere, and a timer that throws forever is
            // not.
            Silence();
        }
    }

    public override void Dispose()
    {
        Silence();

        try
        {
            AudioClose();
        }
        catch
        {
            // Closing a speaker that is already gone.
        }
    }

    private void Silence()
    {
        _closed = true;
        _timer?.Stop();
        _timer = null;
        _fill = null;
    }

    [JSImport("audioOpen", Program.AudioModule)]
    private static partial int AudioOpen();

    /// <summary>
    /// Hands one block over as an array of numbers, copied.
    ///
    /// <b>It used to be a memory view, and that is what was killing the tab.</b> A
    /// <c>JSType.MemoryView</c> is a window straight into the .NET heap, valid only for the duration of the
    /// call — which is the cheapest possible marshalling and exactly right on a single-threaded build. This
    /// head is published with threads, so Avalonia's dispatcher lives on a web worker and this call is made
    /// from that worker while the module it targets was imported on the page's main thread. .NET proxies
    /// the call between the two, and a window into one thread's memory does not survive the crossing. The
    /// symptom was a runtime that aborted on <c>unreachable</c> and exited 1 at unpredictable moments,
    /// always with the sound on and never without it — with a flat heap the whole way, which is what ruled
    /// out the leak everybody looks for first.
    ///
    /// So: a copy. It costs an array of four thousand doubles crossing twelve times a second, and it is the
    /// only kind of argument this boundary can carry between threads without the caller having to know
    /// which thread it is on.
    /// </summary>
    [JSImport("audioPush", Program.AudioModule)]
    private static partial double Push(
        [JSMarshalAs<JSType.Array<JSType.Number>>] double[] block, int frames);

    [JSImport("audioAhead", Program.AudioModule)]
    private static partial double AudioAhead();

    [JSImport("audioTarget", Program.AudioModule)]
    private static partial double AudioTarget();

    [JSImport("audioClose", Program.AudioModule)]
    private static partial void AudioClose();
}

internal sealed partial class Program
{
    /// <summary>
    /// Tells the demo where its sound goes in a tab. See <see cref="BrowserAudio"/>.
    ///
    /// A factory rather than a device, because the demo opens and closes its speaker every time the sound
    /// switch is used and a device that has been disposed cannot be started again.
    /// </summary>
    static partial void InstallHostAudio() => HostAudio.Supply = BrowserAudio.Open;
}
