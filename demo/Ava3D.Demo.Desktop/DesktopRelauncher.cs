using System;
using System.Diagnostics;
using System.Reflection;
using Ava3D.Demo.Engine;
using Avalonia.Controls.ApplicationLifetimes;

namespace Ava3D.Demo.Desktop;

/// <summary>
/// Restarts the demo with a different graphics API, which is the only way to move between Metal and OpenGL.
///
/// Avalonia decides its rendering mode inside <c>AppBuilder</c>, before any control exists, so switching
/// between GPU APIs is a process-lifetime question rather than a control-lifetime one. The switch is carried
/// in the environment because that is what <c>BuildAvaloniaApp</c> already reads — the same two variables that
/// let one machine measure all three backends from a terminal.
///
/// Desktop only, and deliberately so: a browser tab cannot relaunch itself and a mobile application should
/// not try. Those heads leave <see cref="EngineRelauncher.Current"/> null and the demo says why.
/// </summary>
internal sealed class DesktopRelauncher(IControlledApplicationLifetime lifetime) : IEngineRelauncher
{
    public bool CanRelaunch => Executable() != null;

    public string? Unsupported => CanRelaunch
        ? null
        : "the running executable could not be located, so the demo cannot start a replacement.";

    public void Relaunch(RenderBackendKind kind)
    {
        if (Executable() is not { } executable)
            return;

        var start = new ProcessStartInfo(executable) { UseShellExecute = false };

        // Both are cleared and then one is set, so a relaunch out of GL back to Metal does not inherit the
        // flag that put it in GL. Passing an empty value rather than omitting the key matters: the child
        // inherits this process's environment, so "unset" has to be written explicitly.
        start.Environment["AVA3D_GL"] = kind == RenderBackendKind.OpenGL ? "1" : "";
        start.Environment["AVA3D_SOFTWARE"] = "";

        // A probe run is a measurement of one configuration; inheriting it would make the replacement
        // print its summary and exit a few seconds after launch.
        start.Environment["AVA3D_PROBE"] = "";
        start.Environment["AVA3D_CAPTURE"] = "";

        // Carry the current scene across, so a restart to compare renderers does not also lose your place.
        if (Environment.GetEnvironmentVariable("AVA3D_SCENE") is { Length: > 0 } scene)
            start.Environment["AVA3D_SCENE"] = scene;

        try
        {
            Process.Start(start);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Ava3D.Demo] could not relaunch: {e.GetType().Name}: {e.Message}");
            return;
        }

        lifetime.Shutdown();
    }

    /// <summary>
    /// The file to start again.
    ///
    /// <c>Environment.ProcessPath</c> is the apphost — the native launcher a published build produces — which
    /// is what should be started. It is null only in odd hosting arrangements, and the managed assembly path
    /// is the fallback there; <c>Process.Start</c> on a .dll will not work, so that case reports itself as
    /// unsupported instead.
    /// </summary>
    private static string? Executable()
    {
        var path = Environment.ProcessPath;
        if (path is { Length: > 0 } && !path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            return path;

        var entry = Assembly.GetEntryAssembly()?.Location;
        return entry is { Length: > 0 } && !entry.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
            ? entry
            : null;
    }
}
