// Cross-origin isolation, arranged by a page whose host cannot send a header.
//
// The application is published with WasmEnableThreads, and a threaded WebAssembly runtime needs
// SharedArrayBuffer. A browser only hands SharedArrayBuffer to a document that is *cross-origin isolated*,
// which means its own response carried Cross-Origin-Opener-Policy: same-origin and
// Cross-Origin-Embedder-Policy: require-corp. GitHub Pages serves static files and has no way to add a
// header to any of them, so on the face of it the demo's own host cannot run the demo.
//
// A service worker sits between the page and the network and hands back the response itself, so it can add
// the two headers to a response the server never marked. The cost is one reload the first time: the worker
// is not controlling the page that registered it, so that page has to be fetched again to be fetched
// through the worker. After that it is invisible, and it survives the tab being closed.
//
// One file doing two jobs, which is the trick that keeps it honest: the same URL is registered as the
// worker and loaded as a script by the page, so the rule about which responses get marked is written once
// and cannot drift between the half that decides and the half that asks.

if (typeof window === 'undefined') {
    //
    // ---- The worker half -------------------------------------------------------------------------
    //

    // Take over as soon as possible rather than waiting for every tab using the old worker to close, and
    // claim the clients already open. Both are what make the single reload below enough: without them the
    // first visit would install a worker that starts working on the *next* visit.
    self.addEventListener('install', () => self.skipWaiting());
    self.addEventListener('activate', event => event.waitUntil(self.clients.claim()));

    // Where this worker sits, and therefore what the paths below are relative to. Always ends in a slash.
    const scope = new URL(self.registration.scope).pathname;

    // Which responses get the headers, and it is deliberately not "all of them".
    //
    // ?app says everything in scope is the application: the worker was registered by the application's own
    // page from the application's own root, so there is nothing else under it to be careful of.
    //
    // ?site says the worker is scoped over a whole site that happens to contain the demo, and then only two
    // things should be marked — the page that frames the demo and the application under it. A document is
    // isolated by its own response, so marking those two is the whole job, and marking the rest would put
    // require-corp's rules on pages with nothing to gain by it and something to lose: the first
    // cross-origin image, font or embed any of them ever grows would stop loading, silently, on a page
    // nobody was thinking about when they added it.
    const wholeScope = self.location.search !== '?site';

    function marks(url) {
        const target = new URL(url);

        // Never touch another origin's response. Under ?site the paths would not match one anyway, but
        // under ?app they would match everything, and rewriting a response from a server that is not ours
        // is not this worker's business.
        if (target.origin !== self.location.origin)
            return false;

        return wholeScope
            || target.pathname === `${scope}demo.html`
            || target.pathname.startsWith(`${scope}demo/`);
    }

    self.addEventListener('fetch', event => {
        const request = event.request;

        // A request the browser means to answer from its own cache and nowhere else — range requests for
        // media are the usual one. Passing it to fetch() turns it into a request the cache will not answer
        // and the response comes back opaque, so it is left alone.
        if (request.cache === 'only-if-cached' && request.mode !== 'same-origin')
            return;

        if (!marks(request.url))
            return;

        event.respondWith(fetch(request).then(response => {
            // An opaque response has no headers to copy and no body to read. Nothing here can be marked,
            // and building a Response from it would replace it with an empty one.
            if (response.status === 0)
                return response;

            const headers = new Headers(response.headers);
            headers.set('Cross-Origin-Embedder-Policy', 'require-corp');
            headers.set('Cross-Origin-Opener-Policy', 'same-origin');

            // The body is passed through as a stream rather than read: this worker is in front of about
            // twenty-four megabytes of runtime, and buffering it to add two headers would put all of it in
            // memory twice and hold the progress bar at zero until the last byte arrived.
            return new Response(response.body, {
                status: response.status,
                statusText: response.statusText,
                headers
            });
        }));
    });
} else {
    //
    // ---- The page half ---------------------------------------------------------------------------
    //

    // Registering itself, by the URL it was loaded from, which is what keeps the query string above
    // meaningful — ?app and ?site are written in the HTML that includes this file, next to the page that
    // knows which of the two it is. document.currentScript is read now rather than later because it is only
    // set while a classic script is running, which is the other reason this file is not a module.
    const here = document.currentScript && document.currentScript.src;

    // Asked at most once per tab. If the reload does not produce an isolated page — a browser with service
    // workers switched off, an extension in the way, a private window that refuses storage — then asking
    // again produces the same answer, and a page that reloads itself for ever is worse than a page that
    // says what went wrong. main.js is what says it.
    const asked = 'ava3d-coi-reloaded';

    function reloadOnce() {
        try {
            if (sessionStorage.getItem(asked))
                return;
            sessionStorage.setItem(asked, '1');
        } catch {
            // No session storage means no way to remember having asked, so do not ask at all rather than
            // risk a loop.
            return;
        }

        window.location.reload();
    }

    function arrange() {
        if (window.crossOriginIsolated) {
            // Either the worker is already doing its job or the host sent the headers itself. Forget having
            // asked, so that if isolation is ever lost the one reload is available again.
            try { sessionStorage.removeItem(asked); } catch { /* nothing to forget */ }
            return;
        }

        // Only the top-level document can fix this, and an embedded one must not try. Isolation is a
        // property of the whole frame tree — a frame joins its embedder's cluster or it joins nothing — so
        // a frame reloading itself inside an un-isolated page lands back exactly where it was. On the site
        // the demo is framed by demo.html, and demo.html is the one that acts.
        if (window.top !== window)
            return;

        if (!here || !navigator.serviceWorker || !window.isSecureContext)
            return;

        // A page opened from disk cannot register a worker at all, and saying so is index.html's job.
        if (location.protocol !== 'http:' && location.protocol !== 'https:')
            return;

        // Listening before registering, not after. The worker claims its clients as it activates and
        // claiming is what fires this — which on a fast local server can happen while register()'s own
        // promise is still settling, and a listener attached in the .then() would be attached to an event
        // that had already gone past.
        navigator.serviceWorker.addEventListener('controllerchange', reloadOnce);

        navigator.serviceWorker.register(here).then(() => {
            // Already controlling and still not isolated: the worker is from an earlier visit and this
            // document was fetched before it took over, so there is no controllerchange coming.
            if (navigator.serviceWorker.controller)
                reloadOnce();
        }).catch(error => {
            console.warn('[Ava3D.Demo] no service worker, so no threads:', error);
        });
    }

    arrange();
}
