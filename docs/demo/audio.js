// Somewhere for the film's sound to go in a tab.
//
// The demo mixes its own audio — five looping beds and a handful of one-shots, summed into blocks of mono
// float samples — and then needs a speaker. On a desktop that is an audio queue the operating system pulls
// from on a thread of its own. Here there is no thread to be pulled from and no P/Invoke to reach the
// hardware with, so the arrangement is turned around: the application pushes blocks in, and Web Audio plays
// them back to back on a clock that is not the page's.
//
// That inversion is the whole design. A browser's audio clock keeps time whatever the main thread is doing,
// and this demo's main thread is frequently doing eighty milliseconds of software rasterising. Anything
// that asked the page for a sample at the moment it was needed would stutter every frame. Scheduling half a
// second of sound ahead of the speaker means the renderer can miss its timer four times running and nobody
// hears anything.
//
// Nothing in here knows what a voice or a chapter is; it takes samples and a sample rate.
//
// One speaker at a time. The context, the cursor and the set of scheduled sources are module state rather
// than per-speaker, so a second audioOpen() while a first is still running would share all three and
// audioClose() would silence both. That is exactly what this application does — it opens a speaker when the
// sound switch goes on and closes it when the scene is replaced, never two at once — and making it general
// would mean handing back a handle and keeping a map, for a case that does not exist. Written down because
// the failure it would cause is one sound cutting another off, which reads as a bug in the film.

// One context for the life of the page. Browsers cap how many an origin may have — six, in most — and the
// demo opens a speaker every time the sound switch is turned on, so a context per speaker would work for
// the first few visits to the switch and then throw.
let ctx = null;

// Where the next block goes, on the context's clock. Not a queue length: it is a timestamp, so a block
// scheduled while the previous one is still playing lands exactly at its end and the join is sample
// accurate. Falls behind currentTime when the application stops pushing, which is what starve() repairs.
let cursor = 0;

// Everything scheduled and not yet finished, so closing can silence them. A source that has started cannot
// be un-started, only stopped, and one left running outlives the speaker that made it.
let sources = new Set();

// How far ahead of the speaker to keep the schedule.
//
// A fifth of a second is between four and five blocks at the size the application pushes, or two and a half
// frames of the software renderer at its slowest. It was half a second, which was more headroom than the
// main thread has ever been observed to need and which put every one-shot up to half a second behind the
// frame that fired it — see BrowserAudio, where the same number is discussed from the other side. A cue is
// written at the far end of the schedule, so the schedule's depth <i>is</i> the sync error.
const AHEAD = 0.2;

// Opens the speaker and says what rate it wants. 0 means this browser has no audio to offer.
//
// The rate is asked for rather than accepted because the demo generates every sound it plays at whatever
// this returns, and 48 kHz is what the desktop head and the rendered .wav use — one rate everywhere means
// the same arithmetic produces the same waveform, and a bug that only appears at 44.1 cannot hide. A
// browser that will not resample its output device says so by ignoring the request, which is why the answer
// is read back off the context rather than assumed.
export function audioOpen() {
    try {
        if (!ctx) {
            const Context = globalThis.AudioContext ?? globalThis.webkitAudioContext;
            if (!Context)
                return 0;

            ctx = new Context({ sampleRate: 48000, latencyHint: "playback" });

            // A tab will not start an audio context that no one asked for. This one is opened from the
            // sound switch, which is a click, so it usually starts running immediately — but the click
            // arrives here through Avalonia and a WebAssembly call stack, and whether that still counts as
            // a gesture is a judgement each browser makes for itself. So: try to resume now, and leave a
            // listener for the next thing the visitor does in case this browser said no.
            const wake = () => { if (ctx && ctx.state === "suspended") ctx.resume().catch(() => {}); };

            // Capture, and this is the difference between the sound working and not.
            //
            // The application fills the frame, so every gesture a visitor makes lands on Avalonia's canvas
            // — and Avalonia handles pointer events there and stops them. A listener on the window in the
            // bubble phase is therefore a listener on the one part of the page nobody can click: the
            // context stays suspended however many times somebody clicks the picture, and the only gesture
            // that would ever have reached it is one aimed at the margin around the app. Capture runs on
            // the way down, before the canvas gets a say.
            //
            // Once is enough: the context only has to be started, and after that a listener per gesture for
            // the life of the page is work for nothing.
            for (const event of ["pointerdown", "keydown", "touchend"])
                globalThis.addEventListener(event, wake, { capture: true, passive: true });

            // And say so while it is asleep, because the alternative is a demo that answers a switch with
            // silence. Every other head opens its speaker when asked and plays; this one may be refused,
            // and refused invisibly — the switch stays on, the film runs, the console says the speaker was
            // found, and nothing comes out. There is no way to tell that from a bug in the mixer by
            // looking, and the visitor is the only one who can clear it.
            //
            // On statechange rather than polled, and it fires both ways: a browser may suspend a context
            // again when the tab is hidden, and a notice that only ever appeared once would then be a
            // wrong explanation for a second silence.
            ctx.addEventListener("statechange", notice);
            notice();

            wake();
        }

        cursor = 0;
        return ctx.sampleRate;
    } catch {
        // No audio here, which is an ordinary answer rather than a failure: the application falls back to
        // the film without a soundtrack, exactly as it does on a machine with no sound card.
        return 0;
    }
}

// Takes one block of mono samples and schedules it after whatever is already scheduled. Returns how many
// seconds are now waiting to be played, which is how the application knows whether to send another.
//
// `block` is an ordinary array of numbers, copied across the boundary rather than shared with it. It used
// to be a view into the .NET heap, which is faster and cannot be done on a build with threads — see
// BrowserAudio.Push, where the reason is written down next to the crash it caused.
export function audioPush(block, frames) {
    if (!ctx || frames <= 0)
        return 0;

    try {
        const samples = new Float32Array(frames);
        for (let i = 0; i < frames; i++)
            samples[i] = block[i];

        const buffer = ctx.createBuffer(1, frames, ctx.sampleRate);
        buffer.copyToChannel(samples, 0);

        starve();

        const source = ctx.createBufferSource();
        source.buffer = buffer;
        source.connect(ctx.destination);
        source.onended = () => sources.delete(source);
        source.start(cursor);
        sources.add(source);

        cursor += buffer.duration;
        return Math.max(0, cursor - ctx.currentTime);
    } catch {
        return 0;
    }
}

// How much sound is scheduled and not yet heard.
export function audioAhead() {
    if (!ctx)
        return 0;

    starve();
    return Math.max(0, cursor - ctx.currentTime);
}

// How far ahead the application should keep the schedule.
export function audioTarget() {
    return AHEAD;
}

// Silences everything and forgets the schedule, leaving the context open for the next speaker.
export function audioClose() {
    for (const source of sources) {
        try {
            source.stop();
        } catch { /* already finished */ }
    }

    sources.clear();
    cursor = 0;
}

// Shows or hides the line that says the sound is waiting for a gesture.
//
// In the page rather than in the application, because the thing being explained is the page's — a policy of
// this browser about this tab, which the demo can neither satisfy nor detect from inside .NET. Keeping it
// here also keeps it out of the caption band, which belongs to the film and has something to say already.
//
// It is not a button. A button would be the obvious design and it would be a lie: any gesture anywhere
// clears this, including the click that dismissed it, so the honest thing is a label that goes away when
// the visitor does anything at all.
function notice() {
    const waiting = !!ctx && ctx.state === "suspended";
    let line = document.getElementById("audio-wait");

    if (!waiting) {
        line?.remove();
        return;
    }

    if (line)
        return;

    line = document.createElement("p");
    line.id = "audio-wait";
    line.textContent = "Click or press a key to start the sound";
    document.body.appendChild(line);
}

// Catches the cursor up after a gap.
//
// If the application stops pushing — the tab was backgrounded, the renderer hung, the film was paused — the
// cursor is left in the past, and a block scheduled at a time that has already happened is played
// immediately and all at once with every other late block. Two milliseconds of headroom, because a block
// scheduled exactly at currentTime is a block the audio thread may already have walked past.
function starve() {
    if (ctx && cursor < ctx.currentTime + 0.002)
        cursor = ctx.currentTime + 0.002;
}
