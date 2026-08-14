using System.Linq;

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
///
/// Nothing here happens without being asked for first — see <see cref="EngineRelauncher.Ask"/>. Ending the
/// process is the most drastic thing this demo does, and it used to be what a selection in a drop-down did
/// on its own.
/// </summary>
public interface IEngineRelauncher
{
    /// <summary>Whether <see cref="Relaunch"/> will actually do something.</summary>
    bool CanRelaunch { get; }

    /// <summary>
    /// Whether <see cref="Quit"/> will end the process.
    ///
    /// The consolation prize, and it is worth having: a demo that cannot start a copy of itself can still
    /// get out of the way, and the renderer has already been written to the settings file, so opening it
    /// again by hand lands on the one that was asked for. True on desktop and false everywhere else —
    /// a browser tab cannot close itself and an application that exits on its own is a crash to anybody
    /// holding a phone.
    /// </summary>
    bool CanQuit { get; }

    /// <summary>Why not, when <see cref="CanRelaunch"/> is false. Shown to the user verbatim.</summary>
    string? Unsupported { get; }

    /// <summary>
    /// Starts a fresh copy of this application configured for <paramref name="kind"/> and exits.
    /// </summary>
    /// <param name="failure">Why nothing happened, when this returns false. Shown to the user.</param>
    /// <returns>
    /// True when a replacement is running and this process is on its way out — so the caller must not
    /// carry on as if it had a window. False when the copy could not be started at all, which is a thing
    /// the user has to be told rather than a line in a log: they pressed a button expecting the demo to
    /// come back.
    /// </returns>
    bool Relaunch(RenderBackendKind kind, out string? failure);

    /// <summary>Ends the process without starting a replacement. Only when <see cref="CanQuit"/>.</summary>
    void Quit();
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

    /// <summary>
    /// The sentence at the top of the notice over the scene: what happened, in one line, no jargon.
    ///
    /// It lives here rather than in the view because it is the same composition as everything else in
    /// this file — a fact from the library and a fact only the head knows — and because all four things
    /// the demo can say about a renderer being out of reach should be readable in one place. Split across
    /// a view and a catalogue, two of them drift apart and nobody notices until a screenshot.
    /// </summary>
    public static string Headline(BackendOption option) => option switch
    {
        // Reachable, just not from this process. Stated rather than acted on: the demo does not restart
        // itself in answer to a preference it read at start-up, because a restart that cannot fix the
        // problem is a restart that happens again on the way up, forever. The notice offers a button.
        { Availability: BackendAvailability.RequiresRestart } when Current is { CanRelaunch: true } =>
            $"{option.Name} needs the demo restarted.",

        { Availability: BackendAvailability.RequiresRestart } =>
            $"{option.Name} can't be started from inside this app.",

        // Something is missing that could be here. Named in the headline rather than left to the reason
        // underneath, because it is the only one of these four sentences a reader can act on: every other
        // way to be unavailable is a fact about the platform, and telling somebody to install Metal on
        // Windows would be worse than saying nothing.
        { Availability: BackendAvailability.Unavailable, MissingComponents.Count: > 0 } =>
            $"{option.Name} needs {Names(option.MissingComponents)}, which isn't installed.",

        // "Not available here" rather than "not on this platform", because the two Unavailable rows mean
        // different things and only one of them is about the platform. Metal on Windows is a platform
        // fact. Vulkan on a Mac is not — Vulkan runs there perfectly well, and a reader with MoltenVK
        // installed can see that it does. The Reason underneath says which of the two this is, so the
        // headline must not guess.
        _ => $"{option.Name} isn't available here."
    };

    /// <summary>What to tell the user when a backend needs a restart this platform cannot perform.</summary>
    public static string Describe(RenderBackendKind kind, BackendOption option) =>
        Current is { CanRelaunch: true } && option.Availability == BackendAvailability.RequiresRestart
            ? $"{kind} needs a restart. {Explain(option)} Choosing it offers to restart the demo."
            : $"{kind} is out of reach here — {Explain(option)}";

    /// <summary>
    /// The three strings of the confirmation: what is being asked, why, and what the affirmative button
    /// says.
    ///
    /// Together rather than three properties, because they have to agree. The demo ends itself two
    /// different ways — starting a replacement first, or simply going — and a button reading "Yes,
    /// restart" over a question about closing is the kind of thing that only shows up on the one machine
    /// that cannot start a replacement, which is to say never, until somebody's does.
    ///
    /// Asked at all because the alternative was what the demo used to do: pick OpenGL from a list and the
    /// window vanishes. Even when a replacement does come back, a process ending on a selection change is
    /// not something an application is allowed to do without asking.
    /// </summary>
    /// <param name="option">The renderer being asked for.</param>
    /// <param name="failure">
    /// Set when a restart has already been tried and could not start anything, which turns the question
    /// from restarting into closing — the only thing left that gets the user to the renderer they asked
    /// for, since the choice is saved and the next launch will read it.
    /// </param>
    public static (string Question, string Detail, string Yes) Ask(BackendOption option, string? failure = null)
    {
        var detail = failure is { Length: > 0 } ? $"{Explain(option)} {Sentence(failure)}" : Explain(option);

        if (failure is null && Current is { CanRelaunch: true })
            return ($"Restart the demo to use {option.Name}?",
                $"{detail} This copy will close and a new one will open on {option.Name}. Nothing is lost — " +
                "every scene here is built from code.",
                "Yes, restart");

        return ($"Close the demo to use {option.Name}?",
            $"{detail} {option.Name} is saved as the choice either way, so opening the demo again yourself " +
            "will start it on that.",
            "Yes, close");
    }

    /// <summary>
    /// Why a renderer is not available, as sentences, for a message that supplies its own headline.
    ///
    /// Two halves from two places. The library knows why the platform or the process cannot offer the
    /// renderer; only the head knows whether it can restart itself to fix that. Joined here so the demo
    /// gives one answer rather than two half-answers in two panels.
    /// </summary>
    public static string Explain(BackendOption option)
    {
        var reason = Sentence(option.Reason is { Length: > 0 }
            ? option.Reason
            : "this process was started with another graphics API");

        // What to do about it, when there is something. The library keeps the package, the command and
        // the link as separate fields precisely so a host does not have to take its word for the wording
        // — this demo turns them back into a sentence because it has one line to say it in, and a real
        // application with room would make them a button and a link instead.
        if (option.MissingComponents.Count > 0)
            return $"{reason} {Install(option.MissingComponents)}";

        // Whether the demo can restart itself is only part of the answer when a restart would help. Metal
        // on Windows is not a question about this application's lifetime.
        if (option.Availability != BackendAvailability.RequiresRestart || Current is { CanRelaunch: true })
            return reason;

        var blocked = Current?.Unsupported ?? "this platform cannot restart the application from inside it";
        return $"{reason} {Sentence(blocked)}";
    }

    /// <summary>The missing things by name, joined for a sentence: "MoltenVK", or "A and B".</summary>
    private static string Names(IReadOnlyList<MissingComponent> missing) =>
        missing.Count == 1
            ? missing[0].Name
            : string.Join(" and ", missing.Select(m => m.Name));

    /// <summary>
    /// How to get what is missing, as one sentence.
    ///
    /// Both routes where both exist, and in this order: the package first because it is one line in a
    /// csproj, the command second because an application that would rather not carry somebody else's
    /// binary needs to know it has that choice. The URL is left out — this is a line of text in a notice
    /// over a 3D scene, not a document, and a link nobody can click is noise.
    /// </summary>
    private static string Install(IReadOnlyList<MissingComponent> missing)
    {
        var routes = missing
            .Select(m => (m.Name, Package: m.Package, Command: m.Command))
            .Where(m => m.Package is { Length: > 0 } || m.Command is { Length: > 0 })
            .Select(m => m switch
            {
                { Package: { Length: > 0 } p, Command: { Length: > 0 } c } =>
                    $"add the {p} package, or install {m.Name} yourself with \"{c}\"",
                { Package: { Length: > 0 } p } => $"add the {p} package",
                var only => $"install {m.Name} with \"{only.Command}\""
            })
            .ToArray();

        return routes.Length == 0
            ? $"{Names(missing)} has to be on the machine."
            : Sentence($"To fix it, {string.Join("; ", routes)}");
    }

    /// <summary>
    /// A fragment written to be embedded, promoted to a sentence of its own.
    ///
    /// Both halves of that message come from somewhere else — one from the library's backend catalogue and
    /// one from the head — and both are phrased as clauses, so joining them raw produced a lower-case
    /// letter after a full stop.
    ///
    /// The first word is capitalised only if it is entirely lower case already. The reasons in the
    /// catalogue open with things like "macOS defaults to Metal", and MacOS is not a thing.
    /// </summary>
    private static string Sentence(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.Length == 0)
            return trimmed;

        var space = trimmed.IndexOf(' ');
        var firstWord = space < 0 ? trimmed : trimmed[..space];

        if (!firstWord.Any(char.IsUpper))
            trimmed = char.ToUpperInvariant(trimmed[0]) + trimmed[1..];

        return trimmed[^1] is '.' or '!' or '?' ? trimmed : trimmed + ".";
    }
}
