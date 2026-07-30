using System;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using Ava3D.Demo;

[SupportedOSPlatform("browser")]
internal sealed partial class Program
{
    private static async Task Main(string[] args)
    {
        await AdoptQueryStringAsync();

        await BuildAvaloniaApp()
            .WithInterFont()
            .StartBrowserAppAsync("out");
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();

    /// <summary>
    /// Turns <c>?scene=stress&amp;tour=1</c> into the environment variables the shared demo already reads.
    ///
    /// A browser tab has no command line and no shell environment, so the demo's own switches would be
    /// unreachable here — which matters most for the thing the switches exist for: publishing a frame rate
    /// somebody else can reproduce. A link that opens on the benchmark scene makes the number checkable.
    ///
    /// Writing into the process environment rather than inventing a second configuration path keeps one
    /// mechanism for all five heads.
    /// </summary>
    private static async Task AdoptQueryStringAsync()
    {
        try
        {
            // Absolute, not relative: a relative specifier resolves against /_framework/, where the runtime lives,
            // rather than against the site root where wwwroot content is served from.
            await JSHost.ImportAsync(Module, "/ava3d.js");
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
                }
            }
        }
        catch (Exception e)
        {
            // A malformed query string must not stop the application from starting.
            Console.WriteLine($"[Ava3D.Demo] could not read the query string: {e.GetType().Name}: {e.Message}");
        }
    }

    private const string Module = "ava3d";

    [JSImport("locationSearch", Module)]
    private static partial string LocationSearch();
}
