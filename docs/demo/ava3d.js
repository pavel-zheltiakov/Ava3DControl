// The things the demo needs from the page that .NET cannot reach on its own: its switches, where it is
// running, and somewhere to keep a setting.
//
// A browser tab has no command line, so the demo's scene and tour switches — which exist so a published
// frame rate can be reproduced by whoever reads it — arrive as a query string instead. JSImport binds
// functions rather than properties, which is why location.search is wrapped rather than imported directly.
export function locationSearch() {
    return globalThis.location.search ?? "";
}

// Which browser this is, for the diagnostics panel. From inside the sandbox .NET can only report
// "Browser", and a frame rate from a tab means little without knowing whose tab.
//
// Chromium answers directly through userAgentData, and correctly. Everything else has to be read out of a
// user-agent string that has been impersonating its competitors since 2003 — which is why the order below
// is not alphabetical: Edge and Opera both claim to be Chrome further along the same line, and Safari
// claims it at the end of one, so each is tested before the claim it makes.
export function browserName() {
    const nav = globalThis.navigator;
    if (!nav)
        return "";

    const brands = nav.userAgentData?.brands;
    if (brands) {
        const real = brands.find(b => !/not.a.brand/i.test(b.brand));
        if (real)
            return `${real.brand} ${real.version}`;
    }

    const ua = nav.userAgent ?? "";
    const names = [
        ["Edge", /Edg\/(\d+)/],
        ["Opera", /OPR\/(\d+)/],
        ["Firefox", /Firefox\/(\d+)/],
        ["Chrome", /Chrome\/(\d+)/],
        ["Safari", /Version\/(\d+).+Safari/]
    ];

    for (const [name, pattern] of names) {
        const found = pattern.exec(ua);
        if (found)
            return `${name} ${found[1]}`;
    }

    return "";
}

// Which operating system, and which device, for the same panel and the same reason.
//
// Both come out of the user-agent string, which is the only place they exist, and which has been frozen
// for years to stop pages fingerprinting the people reading them. That freeze is why neither answer
// carries a version where a native head would give one: every Windows since 8.1 says "Windows NT 10.0",
// and every macOS since Catalina says "Mac OS X 10_15_7". Saying "Windows" and stopping is the truth; the
// alternative is a version number that is wrong on most machines. Android and iOS were never frozen and
// do carry theirs.
export function platformName() {
    const ua = globalThis.navigator?.userAgent ?? "";
    let found;

    if ((found = /Android (\d+(?:\.\d+)?)/.exec(ua)))
        return `Android ${found[1]}`;

    // "CPU iPhone OS 17_4 like Mac OS X", and "CPU OS 17_4" on an iPad.
    if ((found = /(?:iPhone )?OS (\d+)[_.](\d+)/.exec(ua)) && /iPhone|iPad|iPod/.test(ua))
        return `iOS ${found[1]}.${found[2]}`;

    if (/CrOS/.test(ua))
        return "ChromeOS";
    if (/Windows NT/.test(ua))
        return "Windows";
    if (/Mac OS X/.test(ua))
        return "macOS";
    if (/Linux/.test(ua))
        return "Linux";

    return globalThis.navigator?.userAgentData?.platform ?? "";
}

// The device, where the string names one. Android puts the model in the platform section —
// "(Linux; Android 14; Pixel 8 Pro)" — which is the one place a browser is more forthcoming than a native
// runtime, since neither iOS nor Android hands an application a marketing name. Apple gives the family
// and no more, deliberately, so that is what this reports.
//
// Two tails have to come off that section before the closing bracket: a "Build/…" fingerprint, and the
// "; wv" that marks an embedded WebView. And Chrome since 110 replaces the model outright with the letter
// K on phones it has decided not to identify, which is a placeholder and not a device.
export function deviceName() {
    const ua = globalThis.navigator?.userAgent ?? "";

    const android = /Android [^;)]+;\s*([^;)]+?)(?:\s+Build\/[^;)]*)?(?:;\s*wv)?\)/.exec(ua);
    if (android) {
        const model = android[1].trim();
        if (model && model !== "wv" && model !== "K")
            return model;
    }

    if (/iPhone/.test(ua)) return "iPhone";
    if (/iPad/.test(ua)) return "iPad";

    return "";
}

// Settings that survive a reload.
//
// localStorage rather than a cookie. A cookie is sent to the server with every request for the page and
// every asset on it, which for a setting nothing on the server will ever read is pure postage — and this
// demo is a static site, so there is nobody at the other end to read it anyway. localStorage is scoped to
// the origin, stays until it is cleared, and never leaves the browser.
//
// Both calls are wrapped because reading localStorage is allowed to throw rather than return nothing:
// a browser with site data disabled, a private window under some policies, and a cross-origin iframe all
// raise SecurityError on the property itself. The demo's answer to every one of those is the same — open
// on the default engine — so the failure is swallowed here rather than travelling into .NET as an
// exception about a preference.
const prefix = "ava3d.demo.";

export function settingsGet(key) {
    try {
        return globalThis.localStorage?.getItem(prefix + key) ?? "";
    } catch {
        return "";
    }
}

export function settingsSet(key, value) {
    try {
        if (value)
            globalThis.localStorage?.setItem(prefix + key, value);
        else
            globalThis.localStorage?.removeItem(prefix + key);
    } catch {
        // Nothing to do and nothing worth saying: the demo works, it just will not remember.
    }
}
