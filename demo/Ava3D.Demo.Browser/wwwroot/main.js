// Boots the application, and says how it is going while it does.
//
// The Avalonia template's version is four lines and starts the runtime. The extra work here is all about
// the twenty-odd megabytes in between: counting them as they arrive so the page can show a bar, and saying
// something useful if the runtime never manages to start.

const line = document.getElementById('boot-line');
const bar = document.getElementById('boot-bar');
const hint = document.getElementById('boot-hint');

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
