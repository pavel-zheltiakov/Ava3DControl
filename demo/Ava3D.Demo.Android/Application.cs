using System;
using Android.App;
using Android.Runtime;
using Ava3D.Demo.Engine;
using Avalonia;
using Avalonia.Android;

namespace Ava3D.Demo.Android
{
    [Application]
    public class Application : AvaloniaAndroidApplication<App>
    {
        protected Application(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            NameTheDevice();

            return base.CustomizeAppBuilder(builder)
            .WithInterFont();
        }

        /// <summary>
        /// Tells the diagnostics panel which phone this is.
        ///
        /// The one platform that hands an application the name people call the device by. Everywhere else
        /// the honest answer is the GPU's own name for itself — "Apple A17 Pro GPU", "Mali-G715" — which
        /// the renderer already reports and which the panel falls back to. Here <c>Build</c> has the real
        /// thing, and the release number with it, which is friendlier than the API level .NET reports.
        ///
        /// Manufacturer and model joined, unless the model already opens with the manufacturer: Google
        /// ships "Google" and "Pixel 8", which want joining, while a few vendors put their own name in
        /// both fields and would read as a stutter.
        /// </summary>
        private static void NameTheDevice()
        {
            var maker = (global::Android.OS.Build.Manufacturer ?? "").Trim();
            var model = (global::Android.OS.Build.Model ?? "").Trim();

            if (model.Length > 0 && maker.Length > 0 &&
                !model.StartsWith(maker, StringComparison.OrdinalIgnoreCase))
                model = $"{char.ToUpperInvariant(maker[0])}{maker[1..]} {model}";

            if (model.Length > 0)
                HostPlatform.Hardware = model;

            if (global::Android.OS.Build.VERSION.Release is { Length: > 0 } release)
                HostPlatform.OperatingSystem = $"Android {release}";
        }
    }
}
