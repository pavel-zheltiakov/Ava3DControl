using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Ava3D.Demo.Engine;

/// <summary>
/// Where the demo is running: the machine, the operating system, and the browser when there is one.
///
/// The renderer line says which graphics API is drawing. That is only half of what a number in this panel
/// means — "OpenGL" is the honest answer both in a desktop window and in a browser tab on the same
/// machine, and the frame rate underneath it is not measuring the same thing in the two. So the panel says
/// where it is running as well, in the terms somebody reading it would use: a device, an OS, a browser.
/// Not the class names of whatever handed the context over, which is what used to be there and told a
/// reader nothing they could act on.
///
/// The graphics card comes from the renderer — <see cref="RenderInfo.Device"/> is what the GPU calls
/// itself, and on Apple silicon that is the chip. The device is a separate question, and only two heads
/// can answer it: Android has the model name in <c>Build</c>, and a browser can read one out of the
/// user-agent string. Desktop has none to give, which is why a desktop line opens with the card.
///
/// .NET answers the operating system everywhere except the one place the answer is least guessable. On
/// desktop and mobile <see cref="RuntimeInformation.OSDescription"/> is already the product name and
/// version — macOS 26.5.2, iOS 18.2 — because the runtime asks the platform rather than the kernel.
/// Inside a browser it is the string "Browser": from the sandbox there is no operating system to name, and
/// the browser's own identity lives in <c>navigator</c>, which only JavaScript can reach. So the browser
/// head fills in what it knows and every other head falls through to the runtime.
/// </summary>
public static class HostPlatform
{
    /// <summary>
    /// The device, when the head knows a better name for it than the GPU does — "Pixel 9 Pro", "iPad".
    /// Left null on desktop, where the renderer's own answer is the chip and cannot be improved on.
    /// </summary>
    public static string? Hardware { get; set; }

    /// <summary>The operating system, when the head knows one the runtime cannot see from inside.</summary>
    public static string? OperatingSystem { get; set; }

    /// <summary>The browser and its major version — "Chrome 142". Null everywhere but a browser tab.</summary>
    public static string? Browser { get; set; }

    /// <summary>
    /// One short line naming the host: device, graphics card, operating system, browser — skipping
    /// whatever is not known, which on any given platform is most of them.
    ///
    /// The card is its own item rather than a stand-in for the device, because the two are different
    /// facts and the platforms that have both are the platforms where both are interesting: a phone says
    /// "Pixel 8 Pro · Mali-G715 · Android 14". A desktop has no device name to give and so leads with the
    /// card, which is the thing a frame rate on that machine has to be read against.
    ///
    /// Cheap enough to call per frame, but called once per renderer — nothing in it can change while the
    /// process lives.
    /// </summary>
    /// <param name="graphics">
    /// <see cref="RenderInfo.Device"/>. Null before the first frame, on a host with no graphics context,
    /// and in a browser, which withholds it.
    /// </param>
    public static string Describe(string? graphics = null)
    {
        var parts = new List<string>(4);

        if (Hardware is { Length: > 0 } hardware)
            parts.Add(hardware);

        if (graphics is { Length: > 0 } card)
            parts.Add(card);

        if ((OperatingSystem ?? SystemName()) is { Length: > 0 } os)
            parts.Add(os);

        if (Browser is { Length: > 0 } browser)
            parts.Add(browser);

        return parts.Count > 0 ? string.Join(" · ", parts) : "unknown host";
    }

    /// <summary>
    /// What the runtime says the operating system is, tidied into the name a person would use for it.
    ///
    /// Three platforms need help. Windows has reported the same product name since 2015, so the build
    /// number is the only thing that distinguishes 11 from 10 — 22000 is the first (a Windows Server build
    /// falls between the two and is reported as the client release it is built from, which is wrong and
    /// harmless in a panel about a graphics API). Linux answers with the whole uname line, of which
    /// everything from the build marker on is noise; the distribution's own name is better still and is
    /// one file away. And a browser has no operating system to name from inside the sandbox at all, so
    /// this names the runtime instead — a last resort, reached only when the head's own answer, which is
    /// the real one, did not arrive.
    /// </summary>
    private static string? SystemName()
    {
        // All .NET can see from inside a tab, and only reached when the head's own answer did not arrive.
        if (System.OperatingSystem.IsBrowser())
            return "WebAssembly";

        var os = RuntimeInformation.OSDescription.Trim();

        const string windows = "Microsoft Windows ";
        if (os.StartsWith(windows, StringComparison.Ordinal))
            return Version.TryParse(os[windows.Length..], out var version) && version.Major == 10
                ? $"Windows {(version.Build >= 22000 ? 11 : 10)}"
                : $"Windows {os[windows.Length..]}";

        if (System.OperatingSystem.IsLinux() && PrettyLinuxName() is { Length: > 0 } distribution)
            return distribution;

        var build = os.IndexOf('#');
        return build > 0 ? os[..build].TrimEnd() : os;
    }

    /// <summary>PRETTY_NAME out of /etc/os-release — "Ubuntu 24.04.1 LTS" — or null if it cannot be read.</summary>
    private static string? PrettyLinuxName()
    {
        try
        {
            foreach (var line in File.ReadLines("/etc/os-release"))
                if (line.StartsWith("PRETTY_NAME=", StringComparison.Ordinal))
                    return line["PRETTY_NAME=".Length..].Trim().Trim('"');
        }
        catch (Exception)
        {
            // A container without the file, or one that will not let this process read it. The uname line
            // is the answer then, and a panel is not worth an exception either way.
        }

        return null;
    }
}
