namespace Ava3D.Demo.Story;

/// <summary>
/// <see cref="Speaker"/> for a build that has no player to speak through.
///
/// This is the file the generated public repository compiles instead of <c>Speaker.cs</c>. That copy of
/// the demo restores the control from nuget.org and has no <c>src</c> directory beside it, so
/// <c>AvaMediaPlayer</c> — which is not published — is not there to reference. See the csproj, which
/// chooses between the two on <c>Ava3DNoPlayer</c> — a switch of its own, because whether the player's
/// source is on disk and where Ava3D itself comes from are two separate questions, and the demo the site
/// ships answers them differently: released package, sound intact.
///
/// <see cref="Open"/> returns null, which is the same answer the real one gives on a machine with no audio
/// device, so the film's silent path is not a second path — it is the one that already had to work.
/// Everything below it is unreachable and is here so the rest of the folder compiles unchanged.
/// </summary>
internal sealed class Speaker : IDisposable
{
    /// <summary>Nothing to open. Null is the ordinary answer for "this machine has no sound", and the
    /// soundtrack already knows what to do with it.</summary>
    public static Speaker? Open() => null;

    /// <summary>Nothing to render onto either. See <see cref="Open"/>.</summary>
    public static Speaker? Recording(int sampleRate) => null;

    public int SampleRate => 48000;

    public string Describe() => "nothing";

    public void Record(float seconds) { }

    public void Save(string path) { }

    public bool Muted { get; set; }

    public Voice Play(float[] samples, float volume = 1f, bool loop = false) => new();

    public void StopAll() { }

    public void Dispose() { }
}

/// <summary>A voice that is not sounding. See <see cref="Speaker"/>.</summary>
internal sealed class Voice
{
    public float Volume { get; set; }

    public void Stop() { }
}
