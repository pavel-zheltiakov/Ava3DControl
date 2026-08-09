namespace Ava3D.Demo.Story;

/// <summary>
/// The handful of operations every sound in this film is made of.
///
/// All of it is generated. Nothing is loaded, nothing is decoded, and there is not one audio file in the
/// repository — for the same three reasons the textures are generated: a recording is weight to download,
/// it carries a licence somebody has to honour, and it cannot be read. A reader who wants to know what a
/// footstep on this deck is can see that it is a burst of noise with a resonance under it, which is both
/// what it is and what a footstep is.
///
/// The vocabulary is deliberately about a dozen words wide — noise, a sine, two filters, an envelope and a
/// way to make something loop — because that is enough for a building at night and because a synthesiser
/// with a hundred is a different project. Everything here operates on <c>float[]</c> in place where it can,
/// so a cue reads as a recipe: take noise, make it dull, give it a shape, put a thump under it.
///
/// <b>Determinism.</b> The noise is a seeded generator rather than <see cref="Random"/>, so the film sounds
/// the same on every run and on every machine. That is the same rule the walk and the light show follow,
/// and it is worth as much here: a sound that is different every time cannot be described in a commit
/// message, compared against yesterday, or ruled out as the cause of anything.
/// </summary>
internal static class Tone
{
    /// <summary>
    /// White noise, from a seed.
    ///
    /// Xorshift rather than <see cref="Random"/> for determinism across runtimes — Random's algorithm is
    /// explicitly not guaranteed between versions, and a film that sounds different after a framework
    /// update is a bug report nobody can reproduce.
    /// </summary>
    public static float[] Noise(int count, uint seed)
    {
        var samples = new float[count];
        var state = seed | 1u;

        for (var i = 0; i < count; i++)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            samples[i] = (int)state / 2147483648f;
        }

        return samples;
    }

    /// <summary>Adds a sine of <paramref name="hz"/> at <paramref name="level"/>, with an optional phase in
    /// turns, into <paramref name="into"/>.</summary>
    public static float[] Sine(this float[] into, int rate, float hz, float level, float phase = 0f)
    {
        var step = 2f * MathF.PI * hz / rate;
        var offset = 2f * MathF.PI * phase;

        for (var i = 0; i < into.Length; i++)
            into[i] += MathF.Sin(i * step + offset) * level;

        return into;
    }

    /// <summary>
    /// A sine that slides from <paramref name="from"/> to <paramref name="to"/> across the buffer.
    ///
    /// <b>The phase is integrated rather than computed.</b> Writing <c>sin(2π·f(t)·t)</c> looks like the
    /// obvious thing and is wrong: differentiate it and the instantaneous frequency comes out as
    /// <c>f(t) + t·f′(t)</c>, so a glide from 200 to 800 over a fifth of a second actually ends somewhere
    /// near 1400 and the sound arrives above where it was asked to. Stepping the phase by the current
    /// frequency each sample is the definition, and it costs the same.
    ///
    /// <paramref name="bend"/> shapes the journey rather than its ends: one is a straight line in hertz,
    /// below one moves most of the distance early, above one holds low and rushes the last of it. Pitch is
    /// heard logarithmically, so a straight line in hertz already sounds like it is slowing down.
    /// </summary>
    public static float[] Glide(this float[] into, int rate, float from, float to, float level,
                                float bend = 1f)
    {
        var phase = 0f;

        for (var i = 0; i < into.Length; i++)
        {
            var u = into.Length <= 1 ? 1f : i / (float)(into.Length - 1);
            var hz = float.Lerp(from, to, MathF.Pow(u, bend));

            into[i] += MathF.Sin(phase) * level;
            phase += 2f * MathF.PI * hz / rate;
        }

        return into;
    }

    /// <summary>
    /// One pole down, in place: everything above <paramref name="hz"/> loses six decibels an octave.
    ///
    /// Six an octave is a gentle slope and it is the right one for making things sound far away or dull,
    /// which is nearly every use here. A steeper filter is two of these in a row, which is how the band
    /// passes below are built.
    /// </summary>
    public static float[] Low(this float[] samples, int rate, float hz)
    {
        var a = 1f - MathF.Exp(-2f * MathF.PI * hz / rate);
        var y = 0f;

        for (var i = 0; i < samples.Length; i++)
        {
            y += (samples[i] - y) * a;
            samples[i] = y;
        }

        return samples;
    }

    /// <summary>One pole up, in place: takes the weight out from under something.</summary>
    public static float[] High(this float[] samples, int rate, float hz)
    {
        var a = 1f - MathF.Exp(-2f * MathF.PI * hz / rate);
        var y = 0f;

        for (var i = 0; i < samples.Length; i++)
        {
            y += (samples[i] - y) * a;
            samples[i] -= y;
        }

        return samples;
    }

    /// <summary>A band, from the two of them. Cheap and wide, which is what a click or a footfall wants.</summary>
    public static float[] Band(this float[] samples, int rate, float low, float high) =>
        samples.Low(rate, high).High(rate, low);

    /// <summary>
    /// An exponential fall to silence over <paramref name="seconds"/>, in place, with a short fade in at
    /// the front.
    ///
    /// The fade in is two milliseconds and is not decoration. A buffer that begins mid-waveform begins with
    /// a step, and a step is a click — audible, and exactly the artefact that makes a generated sound
    /// read as "computer" rather than as the thing it is imitating.
    /// </summary>
    public static float[] Fall(this float[] samples, int rate, float seconds, float attack = 0.002f)
    {
        var rise = Math.Max(1, (int)(attack * rate));
        var k = 5f / Math.Max(1e-4f, seconds);

        for (var i = 0; i < samples.Length; i++)
        {
            var t = (float)i / rate;
            var envelope = MathF.Exp(-k * t) * MathF.Min(1f, (float)i / rise);
            samples[i] *= envelope;
        }

        return samples;
    }

    /// <summary>Multiplies everything by <paramref name="level"/>.</summary>
    public static float[] At(this float[] samples, float level)
    {
        for (var i = 0; i < samples.Length; i++)
            samples[i] *= level;

        return samples;
    }

    /// <summary>Scales so the loudest sample is <paramref name="peak"/>. What makes two cues comparable.</summary>
    public static float[] Peak(this float[] samples, float peak)
    {
        var loudest = 0f;

        foreach (var sample in samples)
            loudest = MathF.Max(loudest, MathF.Abs(sample));

        return loudest <= 1e-6f ? samples : samples.At(peak / loudest);
    }

    /// <summary>
    /// Turns a buffer into one that can be looped without a click at the join.
    ///
    /// The seam is the whole problem with a generated loop. The end of the buffer and the start of it are
    /// unrelated instants of noise, so playing one after the other is a discontinuity once per pass —
    /// which at a four-second loop is a soft tick every four seconds, quiet enough to survive review and
    /// loud enough that a listener eventually hears the room ticking.
    ///
    /// The fix is to hand over rather than cut: the last <paramref name="cross"/> seconds are faded out
    /// while a copy of them is faded in over the first, and then the tail is discarded. Equal-power
    /// crossfading rather than linear, because two uncorrelated signals at half amplitude each sum to
    /// about seventy per cent of one — so a linear fade has an audible dip in the middle of the join.
    ///
    /// The returned buffer is shorter than the one given by <paramref name="cross"/> seconds. Generate that
    /// much extra.
    /// </summary>
    public static float[] Seam(this float[] samples, int rate, float cross)
    {
        var fade = Math.Min((int)(cross * rate), samples.Length / 2);

        if (fade <= 0)
            return samples;

        var length = samples.Length - fade;
        var looped = new float[length];

        Array.Copy(samples, looped, length);

        for (var i = 0; i < fade; i++)
        {
            var u = (float)i / fade;
            var going = MathF.Cos(u * MathF.PI * 0.5f);
            var coming = MathF.Sin(u * MathF.PI * 0.5f);

            looped[i] = looped[i] * coming + samples[length + i] * going;
        }

        return looped;
    }

    /// <summary>
    /// Removes any constant offset, in place.
    ///
    /// Filtered noise needs this and nothing else does. A pseudorandom sequence has a small non-zero mean,
    /// and a low-pass filter is a device for removing everything except the mean — so a bed built by
    /// taking noise down to two hundred hertz arrives with a tiny direct offset sitting under it where the
    /// signal used to be. It is inaudible on its own; what it costs is headroom, and it is why a bed that
    /// measures quiet can still be the thing that makes a loud moment clip.
    /// </summary>
    public static float[] Centre(this float[] samples)
    {
        if (samples.Length == 0)
            return samples;

        var sum = 0.0;

        // Summed as a double. At forty-eight thousand samples a second a float accumulator has lost the
        // bottom of its precision long before the end of a four-second bed, which is the one case where
        // this is called.
        foreach (var sample in samples)
            sum += sample;

        var mean = (float)(sum / samples.Length);

        for (var i = 0; i < samples.Length; i++)
            samples[i] -= mean;

        return samples;
    }

    /// <summary>
    /// Slowly opens and closes the level, in place, at about <paramref name="hz"/> — which is very slow.
    ///
    /// What it is for is stopping a steady loop from sounding steady. Air handling, a motor and a bank of
    /// machines all wander a little, and a bed that does not is the difference between a room and a test
    /// tone. Fractions of a hertz, and a depth of a few per cent.
    ///
    /// <b>The rate is rounded to close the loop</b>, exactly as <see cref="Locked"/> does for a tone, and
    /// it is rounded here rather than left to the caller because forgetting is silent and expensive. A
    /// four-second bed wandered at nine hundredths of a hertz ends its loop about a third of the way
    /// through a breath and starts the next one at the beginning of it — a five per cent step in the level,
    /// once every four seconds, forever. That measured as the largest sample-to-sample jump in the whole
    /// film, on a perfectly clean tick every four seconds, and it took a graph to find because every part
    /// that went into it was seamless.
    /// </summary>
    public static float[] Wander(this float[] samples, int rate, float hz, float depth)
    {
        var step = 2f * MathF.PI * Locked(hz, rate, samples.Length) / rate;

        for (var i = 0; i < samples.Length; i++)
            samples[i] *= 1f - depth + depth * (0.5f + 0.5f * MathF.Sin(i * step));

        return samples;
    }

    /// <summary>
    /// The nearest frequency to <paramref name="hz"/> that fits a whole number of cycles into
    /// <paramref name="length"/> samples.
    ///
    /// A tone in a looping bed has to close: end the buffer a third of the way through a cycle and the
    /// join is a step, which is the one artefact <see cref="Seam"/> cannot help with, because crossfading
    /// a tone with a differently-phased copy of itself cancels it rather than hiding the seam. Rounding the
    /// frequency instead costs at most a fraction of a hertz on a four-second loop, which nothing can hear,
    /// and makes the join exact by construction.
    ///
    /// Which is why every tonal part of a bed is added <i>after</i> <see cref="Seam"/> has settled the
    /// length, not before.
    /// </summary>
    public static float Locked(float hz, int rate, int length)
    {
        var cycles = MathF.Max(1f, MathF.Round(hz * length / rate));
        return cycles * rate / length;
    }

    /// <summary>Adds <paramref name="what"/> into <paramref name="into"/> starting at
    /// <paramref name="at"/> seconds, scaled by <paramref name="level"/>. How a cue is built out of parts
    /// that happen at different moments.</summary>
    public static float[] Mix(this float[] into, float[] what, int rate, float at, float level = 1f)
    {
        var start = (int)(at * rate);

        for (var i = 0; i < what.Length; i++)
        {
            var to = start + i;

            if (to >= 0 && to < into.Length)
                into[to] += what[i] * level;
        }

        return into;
    }
}
