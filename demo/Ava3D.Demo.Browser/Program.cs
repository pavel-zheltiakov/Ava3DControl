using System;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using Avalonia.Logging;
using Avalonia.Threading;
using Ava3D;
using Ava3D.Demo;
using Ava3D.Demo.Engine;

[SupportedOSPlatform("browser")]
internal sealed partial class Program
{
    private static async Task Main(string[] args)
    {
        await AdoptPageAsync();

        await BuildAvaloniaApp()
            .WithInterFont()
            .StartBrowserAppAsync("out");

        // The keyboard, taken as soon as there is an application to give it to.
        //
        // Here rather than in the page's own script because this is the first moment the element to focus
        // exists: main.js awaits runMain, which never returns — see below — so anything it did afterwards
        // would run never. See focusApp for why a browser needs telling at all.
        FocusApp();

        ReportWhenSettled();

        // And then never return.
        //
        // StartBrowserAppAsync completes once the application is up, so a Main that simply awaited it would
        // fall off the end — and falling off the end of Main is how a .NET WebAssembly process asks the
        // runtime to exit. The runtime obliges: it throws ExitStatus, which you can watch arrive in the
        // devtools console, and stops dispatching. Managed timers keep firing, so the application looks
        // alive and prints diagnostics about itself, but nothing is ever drawn again.
        await Task.Delay(Timeout.Infinite);
    }

    /// <summary>
    /// Prints the same summary every other head prints, once the tab has settled.
    ///
    /// <see cref="Ava3D.Demo.Views.MainView.Describe"/> exists to be read from a terminal, a devtools console, logcat
    /// or a device log — every head but this one already had something to call it. So a browser was the one
    /// place where a fact about the renderer could only be got at by looking at a panel on a screen, which
    /// is exactly the platform where nobody is looking at the screen: the tab is usually being driven by
    /// <c>tools/cdp.py</c>, which reads the console.
    ///
    /// Seconds rather than immediately, and settable, because the interesting fields are not filled in
    /// until frames have been drawn — a report taken at startup says the view has never rendered.
    /// </summary>
    private static void ReportWhenSettled()
    {
        if (!double.TryParse(Environment.GetEnvironmentVariable("AVA3D_PROBE"), out var seconds) || seconds <= 0)
            return;

        DispatcherTimer.RunOnce(
            () => Console.WriteLine(Ava3D.Demo.Views.MainView.Describe(Ava3D.Demo.Views.MainView.LastInfo)),
            TimeSpan.FromSeconds(seconds));
    }

    /// <summary>
    /// The application, with Avalonia's own diagnostics turned on.
    ///
    /// <c>LogToTrace</c> is not decoration here. A browser tab has no debugger attached and no terminal, so
    /// an exception thrown inside layout or render — which Avalonia logs and swallows rather than letting
    /// escape — leaves no trace at all: the page shows a blank canvas, the runtime keeps running, and every
    /// diagnostic the application prints about itself says nothing is wrong. Routing the log to Trace puts
    /// those messages in the devtools console, which is the only place anyone can see them from.
    /// </summary>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>().LogToTrace(LogEventLevel.Warning);

    /// <summary>
    /// Takes from the page the three things the shared demo cannot ask for itself.
    ///
    /// The first is <c>?scene=stress&amp;tour=1</c>, turned into the environment variables the demo already
    /// reads. A browser tab has no command line and no shell environment, so the demo's own switches would
    /// be unreachable here — which matters most for the thing the switches exist for: publishing a frame
    /// rate somebody else can reproduce. A link that opens on the benchmark scene makes the number
    /// checkable. Writing into the process environment rather than inventing a second configuration path
    /// keeps one mechanism for all five heads.
    ///
    /// The second is where this is running — the browser, the operating system and, on a phone, the device
    /// — for the diagnostics panel. From inside the sandbox .NET can only call all three of them "Browser".
    ///
    /// The third is somewhere to keep a setting. See <see cref="LocalStorage"/>: a tab has no user profile
    /// directory, so the demo's own file store would appear to work and forget everything on reload.
    /// </summary>
    private static async Task AdoptPageAsync()
    {
        try
        {
            // A relative specifier resolves against _framework/, where the runtime lives, rather than against
            // the root of the published output where wwwroot content lands — hence the "..".
            //
            // It used to be "/ava3d.js", which is the same file only as long as the application is the whole
            // origin. On GitHub Pages it is not: the site is served from /Ava3DControl/ and the demo from
            // /Ava3DControl/demo/, so a leading slash asks the domain root for a file that is two directories
            // down and gets an HTML 404 page back, which fails to parse as a module. Going up one from
            // _framework/ is right wherever the application is mounted, because that relationship is the one
            // thing about the layout that publishing guarantees.
            await JSHost.ImportAsync(Module, "../ava3d.js");

            // Where the film's sound goes. Imported here and not where it is used, because an import is
            // asynchronous and opening a speaker is not: the sound switch is a click, and a click cannot
            // await a fetch. It is a separate module rather than more of ava3d.js because it is a separate
            // subject, and it is imported unconditionally because a page may hold a script the application
            // has no use for — which is what the public copy of this demo is, the player it talks to not
            // being published. See BrowserAudio.
            await JSHost.ImportAsync(AudioModule, "../audio.js");

            // Before Avalonia starts, so the demo's first frame already knows which engine was asked for.
            DemoSettings.Store = new LocalStorage();

            // Does nothing at all in the public copy, where the file that implements it is not compiled —
            // an unimplemented partial method and its call are both erased. There is no sound to install
            // there, and this line is how that is said without a conditional.
            InstallHostAudio();

            // What the panel's host line is made of here. Three separate questions because they have three
            // separate answers, any of which the string may not carry: a desktop tab knows no device, and
            // a browser nobody has heard of knows no name for itself.
            HostPlatform.Hardware = Blank(DeviceName());
            HostPlatform.OperatingSystem = Blank(PlatformName());
            HostPlatform.Browser = Blank(BrowserName());

            var query = LocationSearch();
            if (string.IsNullOrEmpty(query))
                return;

            foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var split = pair.Split('=', 2);
                var value = split.Length > 1 ? Uri.UnescapeDataString(split[1]) : "1";

                switch (split[0].ToLowerInvariant())
                {
                    case "scene":
                        Environment.SetEnvironmentVariable("AVA3D_SCENE", value);
                        break;
                    case "tour":
                        Environment.SetEnvironmentVariable("AVA3D_TOUR", value);
                        break;
                    case "story":
                        // ?story=0 is the one that earns its place. The toggle is remembered between
                        // visits, so a tab that was last left in the film opens in the film — and a
                        // report that says "the sphere scene looks wrong here" cannot be reproduced from
                        // a link at all until there is a way to say which of the two to start in.
                        Environment.SetEnvironmentVariable("AVA3D_STORY", value);
                        break;
                    case "at":
                        // ?at=<seconds> opens the film there whatever the picked entry's cue says. The
                        // cues are the moments a feature is on screen, and two of the chapters have no
                        // feature in them at all — so without this there is no link that lands in the
                        // gallery or in the departure, and no way to look at a hand-over between two
                        // chapters in a browser at all.
                        Environment.SetEnvironmentVariable("AVA3D_STORY_AT", value);
                        break;
                    case "sound":
                        // ?sound=1 is the only way a scripted run gets the film to make a noise, since the
                        // switch defaults off and a measured run ignores the remembered setting. It is also
                        // how the tab's speaker gets tested at all: opening it needs a gesture, and there is
                        // nobody there to make one.
                        Environment.SetEnvironmentVariable("AVA3D_SOUND", value);
                        break;
                    case "threads":
                        // ?threads=0 holds the CPU renderer to one core, which is the other half of the
                        // comparison the picker offers — and the half a probe run can take a number from.
                        // This head is published with WasmEnableThreads, so there is more than one core in
                        // the tab to hold it back from.
                        Environment.SetEnvironmentVariable("AVA3D_THREADS", value);
                        break;
                    case "shadows":
                        // ?shadows=0 is the only way to ask a browser run for the scene without its
                        // shadow pass. Every other head has an environment variable for it and a tab has
                        // none, so without this the switch in the toolbar is the only route — which is
                        // fine for looking and useless for a measurement somebody else has to reproduce,
                        // since a probe run ignores the remembered setting by design.
                        Environment.SetEnvironmentVariable("AVA3D_SHADOWS", value);
                        break;
                    case "probe":
                        // ?probe=<seconds> is the desktop head's AVA3D_PROBE, which prints what the
                        // renderer did and exits. Here it only prints: a tab cannot exit without taking
                        // the runtime down with it, and there is nothing to return an exit code to.
                        Environment.SetEnvironmentVariable("AVA3D_PROBE", value);
                        break;
                    case "engine":
                        PreferEngine(value);
                        break;
                }
            }
        }
        catch (Exception e)
        {
            // A malformed query string must not stop the application from starting.
            Console.WriteLine($"[Ava3D.Demo] could not read the query string: {e.GetType().Name}: {e.Message}");
        }
    }

    /// <summary>
    /// Applies <c>?engine=opengl</c>, which is how a link says which renderer it means to show.
    ///
    /// A default rather than an override, and the difference is the whole reason this is three lines
    /// instead of one. Changing renderer in a browser tab is a page reload — see
    /// <see cref="EngineRelauncher"/> — and the reload lands back on the same URL. So a query string that
    /// won every time would be a picker that visibly does nothing: choose the CPU renderer, watch the page
    /// come back, and it is on WebGL 2 again. Deferring to a choice already stored means the link decides
    /// what a first-time visitor sees and the visitor decides everything after that.
    /// </summary>
    private static void PreferEngine(string value)
    {
        if (DemoSettings.Engine is null
            && Enum.TryParse<RenderBackendKind>(value, ignoreCase: true, out var kind)
            && Enum.IsDefined(kind))
        {
            DemoSettings.Engine = kind;
        }
    }

    /// <summary>The name the page's module is imported under. Shared with <see cref="LocalStorage"/>.</summary>
    internal const string Module = "ava3d";

    /// <summary>The name the audio module is imported under. Declared here rather than beside the code that
    /// uses it, because that code is not in every build of this head and the import above is.</summary>
    internal const string AudioModule = "ava3daudio";

    /// <summary>
    /// Implemented by <c>BrowserAudio.cs</c> when there is a player for it to talk to, and by nothing at all
    /// when there is not.
    /// </summary>
    static partial void InstallHostAudio();

    [JSImport("focusApp", Module)]
    private static partial bool FocusApp();

    [JSImport("locationSearch", Module)]
    private static partial string LocationSearch();

    [JSImport("browserName", Module)]
    private static partial string BrowserName();

    [JSImport("platformName", Module)]
    private static partial string PlatformName();

    [JSImport("deviceName", Module)]
    private static partial string DeviceName();

    /// <summary>
    /// Empty to null. The page answers "" for a question it cannot answer, because JSImport marshals a
    /// null string and an empty one identically and one of the two is a lie about what was asked.
    /// </summary>
    private static string? Blank(string value) => value is { Length: > 0 } ? value : null;
}
