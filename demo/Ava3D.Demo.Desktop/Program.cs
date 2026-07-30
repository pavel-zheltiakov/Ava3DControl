using System;
using Ava3D.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace Ava3D.Demo.Desktop;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        FrameCapture.Path = Environment.GetEnvironmentVariable("AVA3D_CAPTURE");
        if (int.TryParse(Environment.GetEnvironmentVariable("AVA3D_CAPTURE_FRAME"), out var frame))
            FrameCapture.Frame = frame;

        // AVA3D_PROBE=<seconds> renders for a while, prints what the renderer did, then exits — so the
        // control can be verified from a terminal instead of by someone watching a window.
        if (double.TryParse(Environment.GetEnvironmentVariable("AVA3D_PROBE"), out var seconds) && seconds > 0)
        {
            var lifetime = new ClassicDesktopStyleApplicationLifetime
            {
                ShutdownMode = ShutdownMode.OnExplicitShutdown
            };

            BuildAvaloniaApp().SetupWithLifetime(lifetime);
            Engine.EngineRelauncher.Current = new DesktopRelauncher(lifetime);

            DispatcherTimer.RunOnce(() =>
            {
                Console.WriteLine(Views.MainView.Describe(Views.MainView.LastInfo));
                lifetime.Shutdown();
            }, TimeSpan.FromSeconds(seconds));

            lifetime.Start(args);
            return;
        }

        var desktop = new ClassicDesktopStyleApplicationLifetime
        {
            ShutdownMode = ShutdownMode.OnLastWindowClose,
            Args = args
        };

        BuildAvaloniaApp().SetupWithLifetime(desktop);

        // Registered before the window is shown, so the engine picker knows from its first frame whether
        // "needs restart" is something this head can actually do.
        Engine.EngineRelauncher.Current = new DesktopRelauncher(desktop);

        desktop.Start(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

        // Ava3D picks its backend from whatever the graphics lease hands back, so on macOS this sample
        // runs on Metal by default, exactly as a real macOS app would. These two switches ask Avalonia
        // for a different graphics API so all three of Ava3D's backends can be measured on one machine:
        //
        //   AVA3D_GL=1        OpenGL 4.1        → GlBackend
        //   AVA3D_SOFTWARE=1  no GPU context    → SkiaBackend
        //   (neither)         Metal             → MetalBackend
        //
        // The software switch earns its keep: with Metal covering macOS and iOS and GL covering
        // everything else, the CPU fallback is now only reached by Vulkan and software hosts, and a
        // path nothing exercises is a path that quietly stops working.
        if (Environment.GetEnvironmentVariable("AVA3D_SOFTWARE") == "1")
        {
            builder = builder.With(new AvaloniaNativePlatformOptions
            {
                RenderingMode = [AvaloniaNativeRenderingMode.Software]
            });
        }
        else if (Environment.GetEnvironmentVariable("AVA3D_GL") == "1")
        {
            builder = builder.With(new AvaloniaNativePlatformOptions
            {
                RenderingMode = [AvaloniaNativeRenderingMode.OpenGl, AvaloniaNativeRenderingMode.Software]
            });
        }

        return builder;
    }
}
