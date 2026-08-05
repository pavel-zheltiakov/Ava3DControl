using System;
using System.Diagnostics;
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
/// What to start is <see cref="RelaunchPlan"/>'s job and is not the same answer on every desktop. All that
/// is left here is the order of the two events, which matters: the replacement is started first and this
/// process only ends if it started. A demo that closes and does not come back is a worse outcome than one
/// that stays open saying it could not restart, and the difference between them is entirely in whether the
/// window goes before or after the news arrives.
///
/// Desktop only, and deliberately so: a browser tab cannot relaunch itself and a mobile application should
/// not try. Those heads leave <see cref="EngineRelauncher.Current"/> null and the demo says why.
/// </summary>
internal sealed class DesktopRelauncher(IControlledApplicationLifetime lifetime) : IEngineRelauncher
{
    public bool CanRelaunch => RelaunchPlan.For(RenderBackendKind.Automatic, LaunchShape.Current) != null;

    /// <summary>
    /// Always, on a desktop. Closing is the fallback when a copy cannot be started, and it is a real one:
    /// the renderer has already been saved, so opening the demo again by hand lands on it.
    /// </summary>
    public bool CanQuit => true;

    public string? Unsupported => CanRelaunch
        ? null
        : "the running executable could not be located, so the demo cannot start a replacement.";

    public bool Relaunch(RenderBackendKind kind, out string? failure)
    {
        if (RelaunchPlan.For(kind, LaunchShape.Current) is not { } start)
        {
            failure = Unsupported;
            return false;
        }

        try
        {
            // Null means the OS handed the work to something already running rather than making a process
            // — not a thing that happens for anything this starts, and worth failing loudly on rather than
            // closing the window and hoping.
            if (Process.Start(start) is not { } child)
            {
                failure = $"{start.FileName} started nothing.";
                return false;
            }

            // Half a second of watching it, which is long enough to catch the two failures that are worth
            // catching and shorter than the window takes to disappear anyway.
            //
            // Starting a process succeeds long before the thing it started works. On macOS the process
            // started is `open`, which does its job in a few milliseconds and reports by exit code whether
            // Launch Services would have it; elsewhere it is the demo itself, which either runs for hours
            // or dies immediately over a missing runtime. Both of those look identical to Process.Start,
            // and both used to end with this window closed and nothing on screen.
            if (child.WaitForExit(500) && child.ExitCode != 0)
            {
                failure = $"the replacement exited immediately with code {child.ExitCode}.";
                return false;
            }
        }
        catch (Exception e)
        {
            failure = $"{start.FileName} could not be started: {e.Message}";
            Console.WriteLine($"[Ava3D.Demo] could not relaunch: {e.GetType().Name}: {e.Message}");
            return false;
        }

        failure = null;
        lifetime.Shutdown();
        return true;
    }

    public void Quit() => lifetime.Shutdown();
}
