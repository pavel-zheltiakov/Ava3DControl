using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ava3D.Demo.Engine;

/// <summary>
/// Somewhere to keep a handful of strings between runs.
///
/// A file is the obvious answer and it is the wrong one in a browser: the .NET filesystem inside the
/// sandbox is memory, so a settings file written there survives exactly as long as the tab. The browser's
/// own answer is <c>localStorage</c>, which only JavaScript can reach — so the shape here is the same one
/// <see cref="EngineRelauncher"/> uses: the shared demo asks, and the head that knows better answers.
/// </summary>
public interface ISettingsStore
{
    /// <summary>The value stored under <paramref name="key"/>, or null when there is none.</summary>
    string? Get(string key);

    /// <summary>Stores <paramref name="value"/>, or forgets the key when it is null.</summary>
    void Set(string key, string? value);
}

/// <summary>
/// What the demo remembers, and where.
///
/// One key today. It is a store rather than a single property because the thing that makes this awkward —
/// three platforms with three different notions of "keep this" — is worth solving once.
///
/// Nothing here may throw. A settings file on a read-only volume, a browser with storage disabled by
/// policy, a phone out of space: every one of those is a reason to open on the default engine, and none of
/// them is a reason for a 3D demo to fail to start.
/// </summary>
public static class DemoSettings
{
    /// <summary>
    /// Where settings are kept. The browser head replaces this before Avalonia starts; every other head
    /// leaves the file store, which persists properly on Windows, macOS, Linux, Android and iOS.
    /// </summary>
    public static ISettingsStore Store { get; set; } = new FileSettings();

    /// <summary>
    /// The renderer the picker was last left on, or null for "never chosen" — which is not the same as
    /// <see cref="RenderBackendKind.Automatic"/>. Automatic is a choice, and choosing it is how you undo
    /// having chosen something else.
    /// </summary>
    public static RenderBackendKind? Engine
    {
        get
        {
            if (Measuring)
                return null;

            return Enum.TryParse<RenderBackendKind>(Read("engine"), ignoreCase: true, out var kind)
                   && Enum.IsDefined(kind)
                ? kind
                : null;
        }

        set => Write("engine", value?.ToString());
    }

    /// <summary>
    /// Whether the demo shows the exhibition or the features on their own. On by default.
    ///
    /// On by default because the default is what somebody who has never heard of this library sees, and a
    /// list of thirty-four unrelated scenes asks them to decide, unpaid, whether to click it thirty-three
    /// more times. Off is the reference: the same list, the same names, each feature against a black
    /// background with nothing else in the frame — which is what somebody reading the source wants, and
    /// nobody else.
    ///
    /// Remembered for the same reason the engine is. It is a decision about how you are using the demo,
    /// not a per-session whim, and re-answering it on every launch is a small tax on the person using it
    /// most.
    ///
    /// A measured run gets the default rather than the file's, exactly as the engine does: a capture or a
    /// probe measures the configuration on its command line, and a figure that depends on what the last
    /// person clicked is not a figure.
    /// </summary>
    /// <remarks>
    /// <c>AVA3D_STORY=1</c> asks for it explicitly and beats everything, which is how a capture or a probe
    /// gets at the film at all: a measured run ignores the settings file by design, so without a switch
    /// there would be no way to photograph a room from a script. Same shape as <c>AVA3D_SOFTWARE</c> and
    /// <c>AVA3D_THREADS</c> — the command line says what is being measured, and nothing else gets a vote.
    /// </remarks>
    public static bool Story
    {
        get
        {
            if (Environment.GetEnvironmentVariable("AVA3D_STORY") is { Length: > 0 } asked)
                return asked is not ("0" or "off" or "false");

            return !Measuring && Read("story") is not "off";
        }

        set => Write("story", value ? "on" : "off");
    }

    /// <summary>
    /// Whether the film is allowed to make a noise. On by default, and remembered.
    ///
    /// It was off, on the argument that a demo which starts drawing is doing what it was opened for while a
    /// demo which starts playing is doing something to the room the person opening it is sitting in — they
    /// may be on a train, in an office, or four minutes into a call — and that the cost of being wrong is
    /// asymmetric, since a silent film is a switch away and a loud one in a quiet office is not undoable.
    ///
    /// That argument is still true and it is no longer the one that decides. The score, the cues and the
    /// synthesis are a third of what this film is and every one of them is generated rather than played
    /// from a file, which is a thing worth hearing and not a thing anyone thinks to go looking for a switch
    /// for. A default nobody finds is a feature nobody sees.
    ///
    /// What makes it affordable is that a browser will not start audio without a gesture anyway, so the
    /// first sound arrives on the first click rather than on load — and turning it off is remembered, so
    /// the train case costs one switch once.
    /// </summary>
    /// <remarks>
    /// <c>AVA3D_SOUND=1</c> forces it, for the same reason <c>AVA3D_STORY</c> exists: a measured run
    /// ignores the settings file, so without a switch there would be no way to ask a scripted run for
    /// audio at all.
    /// </remarks>
    public static bool Sound
    {
        get
        {
            if (Environment.GetEnvironmentVariable("AVA3D_SOUND") is { Length: > 0 } asked)
                return asked is not ("0" or "off" or "false");

            // Absent means on, and only "off" means off — so the default moves without anyone who already
            // turned it off having their choice quietly reversed.
            return !Measuring && Read("sound") is not "off";
        }

        set => Write("sound", value ? "on" : "off");
    }

    /// <summary>
    /// Whether this process is being measured rather than used.
    ///
    /// A probe or capture run measures the configuration it was handed, and its whole value is that
    /// somebody else can reproduce the number from the same command line. A saved preference silently
    /// changing which renderer that command measures would make every figure in the release notes depend
    /// on what the last person to open the demo happened to click. So the switches win, and when there are
    /// no switches the answer is the platform's default rather than the settings file's.
    /// </summary>
    private static bool Measuring =>
        Environment.GetEnvironmentVariable("AVA3D_PROBE") is { Length: > 0 } ||
        Environment.GetEnvironmentVariable("AVA3D_CAPTURE") is { Length: > 0 };

    private static string? Read(string key)
    {
        try
        {
            return Store.Get(key);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Ava3D.Demo] could not read setting '{key}': {e.GetType().Name}: {e.Message}");
            return null;
        }
    }

    private static void Write(string key, string? value)
    {
        try
        {
            Store.Set(key, value);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Ava3D.Demo] could not save setting '{key}': {e.GetType().Name}: {e.Message}");
        }
    }
}

/// <summary>
/// Settings in a text file under the user's application data.
///
/// <c>key=value</c> lines, rewritten whole. There are two of them at most and a demo's settings file is
/// something a person may well want to open and read — a format you can explain in one sentence is worth
/// more here than one that scales.
/// </summary>
/// <param name="path">
/// Where to keep them. Defaults to the user's application data, which is the answer for the demo itself;
/// the parameter is what lets the round trip be checked without writing over somebody's real settings.
/// </param>
internal sealed class FileSettings(string? path = null) : ISettingsStore
{
    private readonly string? _path = path ?? Locate();
    private Dictionary<string, string>? _values;

    public string? Get(string key) => Load().GetValueOrDefault(key);

    public void Set(string key, string? value)
    {
        var values = Load();

        if (value is { Length: > 0 })
            values[key] = value;
        else if (!values.Remove(key))
            return;

        if (_path is null)
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllLines(_path, values.Select(pair => $"{pair.Key}={pair.Value}"));
    }

    private Dictionary<string, string> Load()
    {
        if (_values is not null)
            return _values;

        _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (_path is null || !File.Exists(_path))
            return _values;

        foreach (var line in File.ReadAllLines(_path))
        {
            var split = line.Split('=', 2);
            if (split.Length == 2 && split[0].Trim() is { Length: > 0 } key)
                _values[key] = split[1].Trim();
        }

        return _values;
    }

    /// <summary>
    /// The settings file, or null when this platform has nowhere to put one.
    ///
    /// The browser is the null case: <c>ApplicationData</c> is empty there, and a relative path would
    /// write happily into the in-memory filesystem and lose it on reload. Better to know that this store
    /// cannot keep anything than to appear to work until the tab is closed.
    /// </summary>
    private static string? Locate()
    {
        try
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return root is { Length: > 0 } ? Path.Combine(root, "Ava3D.Demo", "settings.txt") : null;
        }
        catch
        {
            return null;
        }
    }
}
