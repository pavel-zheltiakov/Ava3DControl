using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Ava3D.Demo.Engine;

namespace Ava3D.Demo.Desktop;

/// <summary>
/// How this process was started, as far as starting another one like it is concerned.
///
/// Four things, because there are four ways a .NET desktop application can be sitting on disk and the
/// answer to "start me again" is different for each. Passed in rather than read in, so every shape can be
/// checked on one machine — this Mac cannot be an AppImage, and a machine that can is not the one anybody
/// is sitting at when the code is written.
/// </summary>
/// <param name="ProcessPath">
/// <see cref="Environment.ProcessPath"/> — the native binary that is running. Usually the apphost .NET
/// built beside the assemblies; sometimes the <c>dotnet</c> muxer itself.
/// </param>
/// <param name="EntryAssembly">
/// The managed .dll, which is the argument the muxer needs. Empty in a single-file build, where it is not
/// needed either.
/// </param>
/// <param name="AppImage">
/// <c>$APPIMAGE</c>. Set by the AppImage runtime to the bundle the user actually double-clicked; without
/// it, <see cref="ProcessPath"/> points inside a mount that disappears with this process.
/// </param>
/// <param name="MacOs">Whether to look for an application bundle, which only macOS has.</param>
public readonly record struct LaunchShape(string? ProcessPath, string? EntryAssembly, string? AppImage, bool MacOs)
{
    public static LaunchShape Current => new(
        Environment.ProcessPath,
        Assembly.GetEntryAssembly()?.Location,
        Environment.GetEnvironmentVariable("APPIMAGE"),
        OperatingSystem.IsMacOS());
}

/// <summary>
/// The command that starts a second copy of this demo on a given renderer — or nothing, when this process
/// cannot work out what it is.
///
/// Its own file, and a pure function, because it is the part of restarting that is platform-specific and
/// therefore the part that is wrong on the platform you are not on. Nothing here starts anything; see
/// <see cref="DesktopRelauncher"/>.
///
/// Four shapes:
///
/// <list type="bullet">
/// <item>An <b>AppImage</b> runs from a mount under /tmp that is unmounted when the process ends, so the
/// path it is running from is not a path a replacement can be started from. The runtime leaves the real
/// one in <c>$APPIMAGE</c>.</item>
/// <item>A <b>macOS application bundle</b> is started through <c>open</c>, not by running the binary
/// inside it. Both work, but only <c>open</c> goes through Launch Services, which is what gives the new
/// copy the Dock icon, the menu bar and the front of the screen — a replacement that arrives behind the
/// window you were looking at is indistinguishable from one that never arrived.</item>
/// <item>The <b>dotnet muxer</b> — <c>dotnet Demo.dll</c>, which is how an IDE usually runs a project —
/// is running a process called <c>dotnet</c>. Starting that again with no arguments prints the .NET
/// command-line help to a console nobody is watching and exits, which is a demo that closed and did not
/// come back. It needs the assembly named after it.</item>
/// <item>Everything else is the <b>apphost</b>: the small native launcher built beside the assemblies,
/// which is what <c>dotnet run</c>, a published folder and a Windows shortcut all end up running. Start
/// it again and it does the whole thing.</item>
/// </list>
/// </summary>
public static class RelaunchPlan
{
    /// <summary>
    /// What to run, and with what, to get <paramref name="kind"/> — or null when nothing here fits, which
    /// is what makes <see cref="IEngineRelauncher.CanRelaunch"/> false rather than making a restart a
    /// button that silently does nothing.
    /// </summary>
    public static ProcessStartInfo? For(RenderBackendKind kind, LaunchShape shape)
    {
        var switches = Switches(kind);

        if (shape.AppImage is { Length: > 0 } appImage && File.Exists(appImage))
            return Run(appImage, switches);

        if (shape.MacOs && Bundle(shape.ProcessPath) is { } bundle)
            return Open(bundle, switches);

        if (IsMuxer(shape.ProcessPath) is { } muxer)
            return shape.EntryAssembly is { Length: > 0 } assembly &&
                   assembly.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                ? Run(muxer, switches, assembly)
                : null;

        return shape.ProcessPath is { Length: > 0 } path &&
               !path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
            ? Run(path, switches)
            : null;
    }

    /// <summary>
    /// What the replacement is told, and what it is told to forget.
    ///
    /// Belt as well as braces. The picker has already written the choice to the settings file, which is
    /// what a cold start days later reads; this says the same thing a second way, so a restart lands on
    /// the renderer that was asked for even on a machine where that file cannot be written.
    ///
    /// The empty values are not padding, and they are not empty by the time they arrive either — see
    /// <see cref="Run"/>. A child inherits this process's environment, so a relaunch out of OpenGL back to
    /// Metal would otherwise carry the flag that put it in OpenGL: "unset" has to be written down. And a
    /// probe or capture run is a measurement of one configuration, so inheriting it would make the
    /// replacement print a summary and exit a few seconds after launch.
    /// </summary>
    private static Dictionary<string, string> Switches(RenderBackendKind kind)
    {
        var switches = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AVA3D_GL"] = kind == RenderBackendKind.OpenGL ? "1" : "",
            ["AVA3D_SOFTWARE"] = "",
            ["AVA3D_PROBE"] = "",
            ["AVA3D_CAPTURE"] = ""
        };

        // Carry the current scene across, so a restart to compare renderers does not also lose your place.
        if (Environment.GetEnvironmentVariable("AVA3D_SCENE") is { Length: > 0 } scene)
            switches["AVA3D_SCENE"] = scene;

        return switches;
    }

    /// <summary>
    /// A binary this process can run directly, with the environment it should run with.
    ///
    /// <c>Remove</c> rather than an empty string, because those are two different things and only one of
    /// them is "unset": <see cref="ProcessStartInfo.Environment"/> starts as a copy of this process's, and
    /// assigning "" leaves the child a variable that is present and empty. Every switch here is read as a
    /// string rather than a flag, and one of them — AVA3D_CAPTURE — is a file path that a replacement was
    /// therefore encoding a PNG to, and failing, on every frame from the 120th.
    /// </summary>
    private static ProcessStartInfo Run(
        string executable, Dictionary<string, string> switches, params string[] arguments)
    {
        var start = new ProcessStartInfo(executable) { UseShellExecute = false };

        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);

        foreach (var (key, value) in switches)
        {
            if (value.Length == 0)
                start.Environment.Remove(key);
            else
                start.Environment[key] = value;
        }

        return start;
    }

    /// <summary>
    /// <c>open -n -a Demo.app --env KEY=VALUE</c>.
    ///
    /// The switches go in the arguments because there is no other way in: <c>open</c> hands the launched
    /// application the environment this process is holding, and <c>--env</c> is the only thing that
    /// overrides it. Which is also why the empty ones are passed rather than skipped — <c>--env KEY=</c>
    /// is what clears an inherited value, and leaving it out would relaunch the demo into OpenGL on the
    /// way back out of OpenGL. Checked on macOS 26, since the manual page says what the flag does and
    /// nothing about what it does not do.
    /// </summary>
    private static ProcessStartInfo Open(string bundle, Dictionary<string, string> switches)
    {
        var start = new ProcessStartInfo("/usr/bin/open") { UseShellExecute = false };

        start.ArgumentList.Add("-n");
        start.ArgumentList.Add("-a");
        start.ArgumentList.Add(bundle);

        foreach (var (key, value) in switches)
        {
            start.ArgumentList.Add("--env");
            start.ArgumentList.Add($"{key}={value}");
        }

        return start;
    }

    /// <summary>
    /// The <c>.app</c> directory a path is inside, or null if it is not inside one.
    ///
    /// A bundle's executable lives at <c>Demo.app/Contents/MacOS/Demo</c>, and that is the whole of the
    /// convention: find the marker, keep everything up to the directory that ends in .app. A published
    /// folder on macOS is not a bundle and correctly falls through to being run directly.
    /// </summary>
    private static string? Bundle(string? processPath)
    {
        if (processPath is not { Length: > 0 })
            return null;

        const string inside = "/Contents/MacOS/";
        var marker = processPath.IndexOf(inside, StringComparison.Ordinal);
        if (marker < 0)
            return null;

        var bundle = processPath[..marker];
        return bundle.EndsWith(".app", StringComparison.OrdinalIgnoreCase) ? bundle : null;
    }

    /// <summary>The path to the .NET muxer when that is what is running, or null when it is not.</summary>
    private static string? IsMuxer(string? processPath) =>
        processPath is { Length: > 0 } &&
        string.Equals(Path.GetFileNameWithoutExtension(processPath), "dotnet", StringComparison.OrdinalIgnoreCase)
            ? processPath
            : null;
}
