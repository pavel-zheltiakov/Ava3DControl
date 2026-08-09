// Boots the application, and says how it is going while it does.
//
// The Avalonia template's version is four lines and starts the runtime. The extra work here is all about
// the twenty-odd megabytes in between: counting them as they arrive so the page can show a bar, and saying
// something useful if the runtime never manages to start.

const line = document.getElementById('boot-line');
const bar = document.getElementById('boot-bar');
const hint = document.getElementById('boot-hint');

// ?report=<label> — send this page's console back to the server that is hosting it.
//
// Ava3D.SelfTest prints five checksums over the software renderer's arithmetic twelve seconds in, and the
// only use of them is to diff one engine against another. Chrome will hand its console to a script over
// the DevTools protocol; Safari will not hand it to anything without safaridriver, an administrator and a
// WebDriver session, which is a lot of moving parts between a build and an answer. A page that posts what
// it printed needs none of them, behaves identically in every engine — which matters when comparing two —
// and works on a phone, where there is no console to open at all.
//
// Two guards, and both are the point rather than caution. It has to be asked for, and it only runs against
// a loopback host, so this cannot fire on the published site: there is no server there to post to and no
// visitor who has agreed to it. tools/serve-demo.py is the other end.
const label = new URLSearchParams(globalThis.location.search).get('report');
const local = /^(localhost|127\.0\.0\.1|\[::1\])$/.test(globalThis.location.hostname);

if (label && local) {
    const lines = [];
    let sent = 0;

    // debug as well as log: the runtime routes a managed Console.WriteLine through whichever of the two
    // it was configured with, and which one that is has changed between .NET versions.
    for (const level of ['log', 'info', 'warn', 'error', 'debug']) {
        const inner = console[level].bind(console);
        console[level] = (...args) => {
            lines.push(args.map(a => (typeof a === 'string' ? a : String(a))).join(' '));
            inner(...args);
        };
    }

    // Posted on a timer rather than at the end, because there is no end: the demo runs until the tab is
    // closed, and a report that only arrives on unload is a report that is lost whenever anything hangs.
    // Each post replaces the file, so the last one to land is the whole console.
    setInterval(() => {
        if (lines.length === sent)
            return;

        sent = lines.length;
        fetch(`/report/${encodeURIComponent(label)}`, { method: 'POST', body: lines.join('\n') })
            .catch(() => { /* the server went away; the console still has it */ });
    }, 2000);

    // And the picture, which is the other half of comparing two engines and the half no checksum can
    // stand in for. The fault this is all for is a shape — scattered wrongly-coloured triangles — and a
    // shape has to be looked at. Avalonia draws into a canvas, and a canvas can hand over its own pixels.
    setInterval(() => {
        // The largest, not the first. Avalonia keeps more than one canvas around and the one it draws
        // into is not reliably first in the document — an early grab came back as an untouched 300×150.
        const canvas = [...document.querySelectorAll('canvas')]
            .reduce((best, c) => (!best || c.width * c.height > best.width * best.height ? c : best), null);

        if (!canvas || canvas.width * canvas.height < 10000)
            return;

        // toDataURL taints on a WebGL canvas without preserveDrawingBuffer and throws on a cross-origin
        // one; neither is fatal here, it just means this engine will not give up its frame this way.
        let png;
        try {
            png = canvas.toDataURL('image/png');
        } catch {
            return;
        }

        fetch(`/shot/${encodeURIComponent(label)}`, { method: 'POST', body: png }).catch(() => {});
    }, 4000);
}

let loaded = 0;
let total = 0;

function megabytes(bytes) {
    return (bytes / 1048576).toFixed(1);
}

function report() {
    if (!total) {
        line.textContent = `Downloading — ${megabytes(loaded)} MB`;
        return;
    }

    const fraction = Math.min(1, loaded / total);
    bar.style.width = `${(fraction * 100).toFixed(1)}%`;
    line.textContent = `Downloading — ${megabytes(loaded)} of ${megabytes(total)} MB`;
}

function failed(what, detail) {
    document.querySelector('.boot')?.classList.add('failed');
    line.textContent = what;
    hint.innerHTML = `${detail} <a href="../index.html">Back to Ava3DControl</a>`;
}

// Count the bytes as the runtime pulls them in.
//
// The body has to be re-wrapped rather than read, because the runtime needs it too — a stream can only be
// consumed once. Anything that is not part of the runtime is passed straight through: the demo fetches its
// own models later, and folding those into a bar labelled "downloading the renderer" would be a lie that
// makes the bar go backwards.
const inner = globalThis.fetch;
globalThis.fetch = async (input, init) => {
    const response = await inner(input, init);
    const url = typeof input === 'string' ? input : (input && input.url) || '';

    if (!/_framework\//.test(url) || !response.body || !response.ok)
        return response;

    const reader = response.body.getReader();
    const counted = new ReadableStream({
        async pull(controller) {
            const { done, value } = await reader.read();
            if (done) {
                controller.close();
                return;
            }
            loaded += value.byteLength;
            report();
            controller.enqueue(value);
        },
        cancel(reason) {
            return reader.cancel(reason);
        }
    });

    return new Response(counted, {
        status: response.status,
        statusText: response.statusText,
        headers: response.headers
    });
};

// The denominator, written at publish time by tools/build-demo.sh. A missing or unreadable boot.json is not
// worth failing over — the bar just shows megabytes downloaded instead of a fraction of them.
try {
    const measured = await inner('./boot.json', { cache: 'no-cache' });
    if (measured.ok)
        total = (await measured.json()).bytes || 0;
} catch {
    total = 0;
}

report();

try {
    // Threads, checked before the runtime is fetched rather than after.
    //
    // This application is published for them, so the runtime it is about to download is a shared-memory
    // one and cannot start without SharedArrayBuffer — there is no single-threaded half of it to fall back
    // to. What the runtime does about that on its own is assert somewhere inside twenty-four megabytes of
    // WebAssembly, which is a true answer arriving far too late and in the wrong place. coi.js should have
    // arranged isolation and reloaded before this line was reached; if it is still false here, it could
    // not, and the visitor should be told so before their connection is spent finding out.
    if (!globalThis.crossOriginIsolated)
        throw new Error('the page is not cross-origin isolated');

    const { dotnet } = await import('./_framework/dotnet.js');

    const runtime = await dotnet
        .withDiagnosticTracing(false)
        .withApplicationArgumentsFromQuery()
        .create();

    line.textContent = 'Starting the renderer…';
    bar.style.width = '100%';

    // One click before the film starts, and it is here for exactly one reason: sound.
    //
    // A browser will not let a page make a noise until somebody has interacted with it, and that rule is
    // about the document rather than about any one call — once a visitor has clicked anything, the page has
    // what the specification calls sticky activation and an audio context opened afterwards comes up
    // running. Before that, it does not, and there is nothing the application can do about it from inside.
    //
    // Which is why the wait is here and not in the application. The film opens on a dark room with one lamp
    // and a score under it, and starting it the instant the runtime lands means the first thing anybody
    // sees is that opening playing silently, with the sound arriving whenever they happen to touch
    // something. The score is a third of what this demo is; it should not be a thing you discover late.
    //
    // The splash is already on screen and already explaining itself, so this costs no extra furniture — one
    // line changes and the click that dismisses it is the gesture. Any key does as well as any click,
    // because somebody who has just pressed W is not looking for a button.
    // Not in a scripted run. ?report= is a headless browser posting its console to the server that served
    // it, and there is nobody there to click anything — gating that would turn every automated check into
    // a timeout whose cause is a button nobody can see.
    if (!label) {
        line.textContent = 'Click or press any key to start';
        hint.textContent = '';

        await new Promise(resolve => {
            const begin = () => {
                for (const event of ['pointerdown', 'keydown', 'touchend'])
                    globalThis.removeEventListener(event, begin, { capture: true });

                resolve();
            };

            for (const event of ['pointerdown', 'keydown', 'touchend'])
                globalThis.addEventListener(event, begin, { capture: true, passive: true });
        });

        line.textContent = 'Starting the renderer…';
        hint.textContent = '';
    }

    await runtime.runMain(runtime.getConfig().mainAssemblyName, [globalThis.location.href]);
} catch (error) {
    // A backstop rather than the answer. Opening the published folder from disk is by far the most common
    // way to see a failure here, but on every current browser this file is not loaded at all in that case —
    // a module script is fetched with CORS and a disk page's origin is null — so index.html says it in a
    // classic script instead. This stays for anything that does get here with a file:// URL.
    if (globalThis.location.protocol === 'file:')
        failed('This page has to be served over http.',
            'A file:// page cannot fetch the runtime it needs. Serve the folder — <code>python3 -m ' +
            'http.server</code> in it will do — or open the hosted demo.');
    else if (!globalThis.crossOriginIsolated)
        failed('This page could not be given the threads the demo runs on.',
            'The renderer is compiled for threads, and a browser will only share memory between them on a ' +
            'page that is <em>cross-origin isolated</em>. This host cannot set the two headers that say so, ' +
            'so <code>coi.js</code> asks a service worker to add them — and this browser has refused the ' +
            'worker, or been told to. Turning service workers back on for this site, or leaving a private ' +
            'window, is usually the whole of it.');
    else
        failed('This browser could not start the demo.',
            `It needs WebAssembly and WebGL 2. ${(error && error.message) || error || ''}`);

    console.error('[Ava3D.Demo] the runtime did not start', error);
}
