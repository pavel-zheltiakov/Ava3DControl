using System;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using Avalonia.Logging;
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

            // Before Avalonia starts, so the demo's first frame already knows which engine was asked for.
            DemoSettings.Store = new LocalStorage();

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
                    case "threads":
                        // ?threads=0 holds the CPU renderer to one core, which is the other half of the
                        // comparison the picker offers — and the half a probe run can take a number from.
                        // This head is published with WasmEnableThreads, so there is more than one core in
                        // the tab to hold it back from.
                        Environment.SetEnvironmentVariable("AVA3D_THREADS", value);
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
