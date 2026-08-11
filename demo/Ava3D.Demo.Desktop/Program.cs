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

#if !AVA3D_FROM_PACKAGE
        // AVA3D_CAPTURE_SCALE=2 renders the frame at twice the display's scale, which is how a picture
        // bigger than this screen gets taken. Only useful with AVA3D_CAPTURE; harmless without it.
        //
        // Behind the switch because FrameCapture.Scale is newer than the published package, and this same
        // file is compiled against that package in the public repository. See the csproj.
        if (double.TryParse(Environment.GetEnvironmentVariable("AVA3D_CAPTURE_SCALE"), out var scale) && scale > 0)
            FrameCapture.Scale = scale;
#endif

        // AVA3D_WALK=1 checks every scripted camera in the film against the same solids the free walk is
        // stopped by, prints what clips, and exits without opening a window.
        //
        // It needs no renderer because it is not about rendering: the building is a scene graph, the walk is
        // a pure function of the clock, and whether a pose is inside a bench is arithmetic on two boxes. See
        // Story.Ground.Audit, and see there for why the answer is a report to act on rather than a camera
        // that gets pushed out of the way.
        if (Environment.GetEnvironmentVariable("AVA3D_WALK") == "1")
        {
            // Avalonia is set up but never started, and no window is ever made. The building is not a
            // picture here, it is a graph — but two of its rooms load a glTF, and a loader needs the asset
            // resolver that only exists once the framework has been configured.
            BuildAvaloniaApp().SetupWithoutStarting();

            Console.WriteLine(Views.MainView.DescribeWalks());
            return;
        }

        // AVA3D_SOUND_RENDER=<path> writes the film's soundtrack to a .wav and exits, with no window, no
        // renderer and no audio device. AVA3D_SOUND_FROM and AVA3D_SOUND_TO narrow it to a stretch of film.
        //
        // It is AVA3D_CAPTURE for the other half of the demo, and it is here for the same reason: the only
        // report anybody can give about a sound is that it did or did not sound right where they were
        // sitting, and a file is something a second person can open. Ten minutes of film takes a few
        // seconds, because nothing is drawn.
        if (Environment.GetEnvironmentVariable("AVA3D_SOUND_RENDER") is { Length: > 0 } wav)
        {
            // Same reason as the walk audit above: two of the rooms load a glTF, and a loader needs the
            // asset resolver that only exists once Avalonia has been configured. No window is ever made.
            BuildAvaloniaApp().SetupWithoutStarting();

            float.TryParse(Environment.GetEnvironmentVariable("AVA3D_SOUND_FROM"), out var soundFrom);
            float.TryParse(Environment.GetEnvironmentVariable("AVA3D_SOUND_TO"), out var soundTo);

            Console.WriteLine(Story.StoryScene.RecordSoundtrack(wav, soundFrom, soundTo));
            return;
        }

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

        // Ava3D picks its backend from whatever the graphics lease hands back, so this sample runs on
        // whatever its platform starts with — Metal on macOS, GLX on Linux — exactly as a real
        // application would. These switches ask Avalonia for a different graphics API instead, so all
        // four of Ava3D's backends can be reached from a command line:
        //
        //   AVA3D_SOFTWARE=1  no GPU context    → SkiaBackend
        //   AVA3D_GL=1        OpenGL / GLX      → GlBackend
        //   AVA3D_VULKAN=1    Vulkan            → VulkanBackend   (not on macOS; see below)
        //   (none)            the default       → MetalBackend on macOS, GlBackend elsewhere
        //
        // The software switch earns its keep: with Metal covering macOS and iOS, GL covering everything
        // else and Vulkan needing to be asked for, the CPU fallback is only reached by a host that wants
        // it — and a path nothing exercises is a path that quietly stops working.
        //
        // The engine picker's saved choice is read here too, for the two options a running process cannot
        // reach on its own. Those are the ones that have to be answered before the application is built;
        // Metal is the default on the platforms that have it, and the CPU fallback is the control's own
        // decision, made per frame from the same saved setting in MainView. The environment wins where it
        // speaks, because a probe or capture run is measuring the configuration it was handed and must not
        // inherit whatever the last person to open the demo clicked.
        var wanted =
            Environment.GetEnvironmentVariable("AVA3D_SOFTWARE") == "1" ? RenderBackendKind.Software :
            Environment.GetEnvironmentVariable("AVA3D_GL") == "1" ? RenderBackendKind.OpenGL :
            Environment.GetEnvironmentVariable("AVA3D_VULKAN") == "1" ? RenderBackendKind.Vulkan :
            Engine.DemoSettings.Engine is RenderBackendKind.OpenGL or RenderBackendKind.Vulkan
                ? Engine.DemoSettings.Engine
                : RenderBackendKind.Automatic;

        if (wanted == RenderBackendKind.Automatic)
            return builder;

        // All three platforms' options are set, not just this one's. Only the platform actually running
        // reads its own, so naming the other two costs nothing and means the switches mean the same thing
        // on every desktop rather than being a macOS feature that silently does nothing on Linux — which
        // is what they were, and is why the engine picker on Linux offered a restart into Vulkan that
        // landed straight back on GLX.
        //
        // Each list keeps the software renderer on the end as a fallback, except Vulkan's: a run asking
        // for Vulkan is asking to measure Vulkan, and quietly getting a CPU frame buffer instead is the
        // failure this whole exercise exists to make visible.
        builder = builder
            .With(new X11PlatformOptions
            {
                RenderingMode = wanted switch
                {
                    RenderBackendKind.Software => [X11RenderingMode.Software],
                    RenderBackendKind.Vulkan => [X11RenderingMode.Vulkan],
                    _ => [X11RenderingMode.Glx, X11RenderingMode.Egl, X11RenderingMode.Software]
                }
            })
            .With(new Win32PlatformOptions
            {
                RenderingMode = wanted switch
                {
                    RenderBackendKind.Software => [Win32RenderingMode.Software],
                    RenderBackendKind.Vulkan => [Win32RenderingMode.Vulkan],
                    _ => [Win32RenderingMode.Wgl, Win32RenderingMode.AngleEgl, Win32RenderingMode.Software]
                }
            });

        // Apple has no Vulkan mode to ask for, so asking leaves the platform on its default rather than
        // naming a mode that does not exist. BackendCatalog already reports Vulkan as unavailable there,
        // and this is the same statement made where it has to be acted on.
        return wanted == RenderBackendKind.Vulkan
            ? builder
            : builder.With(new AvaloniaNativePlatformOptions
            {
                RenderingMode = wanted == RenderBackendKind.Software
                    ? [AvaloniaNativeRenderingMode.Software]
                    : [AvaloniaNativeRenderingMode.OpenGl, AvaloniaNativeRenderingMode.Software]
            });
    }
}
