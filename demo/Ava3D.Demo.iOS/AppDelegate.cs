using System;
using Foundation;
using UIKit;
using Avalonia;
using Avalonia.Controls;
using Avalonia.iOS;
using Avalonia.Media;

namespace Ava3D.Demo.iOS;

// The UIApplicationDelegate for the application. This class is responsible for launching the 
// User Interface of the application, as well as listening (and optionally responding) to 
// application events from iOS.
[Register("AppDelegate")]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public partial class AppDelegate : AvaloniaAppDelegate<App>
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        builder = base.CustomizeAppBuilder(builder).WithInterFont();

        // iOS defaults to Metal in Avalonia 12, so the graphics lease hands back a Metal context and
        // Ava3D takes its Skia fallback — correct, but CPU rasterisation at phone resolution is slow.
        // AVA3D_GL=1 asks for OpenGL ES first, which is the opt-in a host application would make.
        //
        // On the simulator: SIMCTL_CHILD_AVA3D_GL=1 xcrun simctl launch …
        //
        // Metal stays in the list as the fallback, so a device that refuses a GL ES context still
        // starts — Ava3D then detects Metal and takes its Skia path, which is the point of detecting
        // the backend at runtime rather than choosing it at build time.
        if (Environment.GetEnvironmentVariable("AVA3D_GL") == "1")
        {
            builder = builder.With(new iOSPlatformOptions
            {
                RenderingMode = [iOSRenderingMode.OpenGl, iOSRenderingMode.Metal]
            });
        }

        return builder;
    }
}
