namespace Ava3D.Demo.Engine;

/// <summary>
/// A host's ability to restart itself with a different graphics API.
///
/// The control can move between the GPU and the CPU on its own, but it cannot move a running process from
/// Metal to OpenGL: Avalonia fixes that when the application is built. Only the head knows how it was
/// launched and whether it can be launched again, so the head supplies this and the shared demo asks.
///
/// A desktop head can do it. A browser tab and a mobile app cannot, and say so through
/// <see cref="Unsupported"/> rather than offering a control that silently does nothing.
/// </summary>
public interface IEngineRelauncher
{
    /// <summary>Whether <see cref="Relaunch"/> will actually do something.</summary>
    bool CanRelaunch { get; }

    /// <summary>Why not, when <see cref="CanRelaunch"/> is false. Shown to the user verbatim.</summary>
    string? Unsupported { get; }

    /// <summary>Starts a fresh copy of this application configured for <paramref name="kind"/> and exits.</summary>
    void Relaunch(RenderBackendKind kind);
}

/// <summary>
/// Where the head leaves its relauncher, if it has one.
///
/// A static hook rather than dependency injection because there is exactly one process and exactly one
/// answer, and because a demo should not need a container to demonstrate a renderer.
/// </summary>
public static class EngineRelauncher
{
    public static IEngineRelauncher? Current { get; set; }

    /// <summary>What to tell the user when a backend needs a restart this platform cannot perform.</summary>
    public static string Describe(RenderBackendKind kind, string? libraryReason)
    {
        var reason = libraryReason is { Length: > 0 } ? libraryReason : "this process was started with another graphics API";

        if (Current is { CanRelaunch: true })
            return $"{kind} needs a restart — {reason}. Selecting it will relaunch the demo.";

        var blocked = Current?.Unsupported ?? "this platform cannot restart the application from inside it";
        return $"{kind} is out of reach here — {reason}. {blocked}";
    }
}
