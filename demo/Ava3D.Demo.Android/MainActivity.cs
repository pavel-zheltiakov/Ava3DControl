using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;

namespace Ava3D.Demo.Android;

[Activity(
    Name = "com.ava3dcontrol.demo.MainActivity",
    Label = "Ava3D.Demo.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    /// <summary>
    /// The demo's switches, which on every other head are environment variables.
    ///
    /// A desktop head is started from a shell and a browser tab has a query string, so both can be told
    /// which scene to open on. Android has neither: an application is launched by tapping it, and there is
    /// nothing to type into. What it does have is intent extras, which is what this reads — so the same
    /// switches are reachable from a machine with the phone plugged into it:
    ///
    /// <code>
    /// adb shell am start -S -n com.ava3dcontrol.demo/.MainActivity --es scene contact --es tour 1
    /// </code>
    ///
    /// <c>-S</c> stops the running instance first, because extras only reach a fresh start. The activity
    /// is given a name of its own rather than left to the generated one for the same reason the command
    /// above is short enough to type: a hashed class name is not something anyone should have to look up
    /// before they can start the application the way they need it.
    ///
    /// Before <c>base.OnCreate</c>, which is where Avalonia is initialised and the view that reads these
    /// is built.
    /// </summary>
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        Switches();
        base.OnCreate(savedInstanceState);
    }

    private void Switches()
    {
        foreach (var (extra, variable) in new[]
        {
            ("scene", "AVA3D_SCENE"),
            ("tour", "AVA3D_TOUR"),
            ("threads", "AVA3D_THREADS"),
            ("captions", "AVA3D_CAPTIONS"),
        })
        {
            if (Read(extra) is { Length: > 0 } value)
                System.Environment.SetEnvironmentVariable(variable, value);
        }
    }

    /// <summary>
    /// One extra, however it was passed. <c>--es name 1</c> and <c>--ez name true</c> both work, because
    /// which of the two you reach for depends on the switch and nobody should have to remember which.
    /// </summary>
    private string? Read(string name)
    {
        try
        {
            if (Intent?.HasExtra(name) != true)
                return null;

            return Intent.GetStringExtra(name) ?? (Intent.GetBooleanExtra(name, false) ? "1" : "0");
        }
        catch (Exception e)
        {
            // A malformed extra must not stop the application from starting.
            Console.WriteLine($"[Ava3D.Demo] could not read the '{name}' extra: {e.GetType().Name}");
            return null;
        }
    }
}
