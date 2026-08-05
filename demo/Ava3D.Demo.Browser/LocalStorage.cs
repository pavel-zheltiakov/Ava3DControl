using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using Ava3D.Demo.Engine;

/// <summary>
/// The browser's answer to "keep this between visits".
///
/// The shared demo's file store is right on every platform that has a filesystem, and useless on this one:
/// .NET inside the sandbox writes into memory, so a settings file there is forgotten the moment the tab
/// closes — which is exactly the bug this whole change is about, arriving by a different route. The page
/// has real storage and only JavaScript can reach it, so the head fills it in, the same way it already
/// fills in the browser's own name.
///
/// Installed before Avalonia starts, so the demo's first frame already knows which engine to ask for.
/// </summary>
[SupportedOSPlatform("browser")]
internal sealed partial class LocalStorage : ISettingsStore
{
    public string? Get(string key) => SettingsGet(key) is { Length: > 0 } value ? value : null;

    public void Set(string key, string? value) => SettingsSet(key, value ?? "");

    // JSImport cannot bind a nullable string either way, and the JavaScript side has its own reasons to
    // return a string for "nothing" — see ava3d.js, where a storage read is allowed to throw. So the empty
    // string is the wire's null in both directions and the translation happens above.
    [JSImport("settingsGet", Program.Module)]
    private static partial string SettingsGet(string key);

    [JSImport("settingsSet", Program.Module)]
    private static partial void SettingsSet(string key, string value);
}
