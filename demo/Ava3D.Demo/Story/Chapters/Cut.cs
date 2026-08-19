using Ava3D.Demo.Scenes.Contact;

namespace Ava3D.Demo.Story;

/// <summary>
/// Chapter 11. The only cut in the film.
///
/// For nine and a half minutes the camera has been a man's eye and every change of shot has been him
/// turning his head. Here it stops being his. He is at the last window watching an escort hold station in
/// front of a planet, and the picture cuts to a film <i>about</i> that: Ashfall from three thousand
/// metres, the convoy he is on, the three ships waiting in the shadow to take it, and the sixty seconds
/// in which that is settled.
///
/// <b>The cut is earned by never having taken one.</b> A film with cuts in it spends nothing to make
/// another one; this one has spent nine minutes not cutting, so the single frame where the picture jumps
/// is the loudest thing in it. That is the whole of the technique and it is why the walk was written the
/// way it was.
///
/// <b>And it cuts planet to planet.</b> Chapter 7 ends looking down thirteen metres of glass at Ashfall
/// across the frame; this one opens on Ashfall from outside, nightside, with the cities on it. Same
/// world, same minute, two scales — which is what turns a jump into a reveal instead of a mistake.
///
/// <b>Nothing here is new.</b> <see cref="ContactScene"/> is the demo's own sixty-second film, mounted
/// exactly as it ships and running its own shot list, its own flight model and its own lighting rig. What
/// changed is what precedes it: the reason that freighter can fly is that somebody fixed its board four
/// minutes ago.
/// </summary>
internal sealed class Cut(ContactScene film, Curtain curtain) : Chapter
{
    /// <summary>
    /// How long the picture takes to go out at the end, in seconds.
    ///
    /// The film's own last shot is a slow crane onto the station with the sun coming off the planet's
    /// limb behind it, and it holds for six seconds with nothing in it changing but the light. Three of
    /// those are spent going to black, which is the only place in the whole exhibition where the picture
    /// is taken away rather than walked out of. It is here rather than at the top of chapter 9 because a
    /// fade belongs to the shot it is fading, and because the two chapters have to meet with the curtain
    /// fully down on both sides of the boundary — see <see cref="Morning"/>, which opens on black and puts
    /// two words on it.
    ///
    /// <b>Nothing about <see cref="ContactScene"/> changes to make this happen.</b> The scene goes on
    /// running its own shot list into its own last frame; what is over the top of it belongs to the story
    /// that mounted it, which is the same arrangement every other thing this chapter does to the film.
    /// </summary>
    private const float Dusk = 3f;

    public override string Title => "Contact";

    public override float Duration => ContactScene.Length;

    /// <summary>
    /// No walk, and that is the chapter.
    ///
    /// Every other chapter answers "where is he standing" and this one has no answer, because he is not
    /// standing anywhere — he is watching. See <see cref="Chapter.Walk"/> for what the null is worth
    /// elsewhere: it is also what keeps the collision audit from asking whether a camera three thousand
    /// metres up is inside a wall, and what stops the film trying to hand the controls to a body that has
    /// no deck under it.
    /// </summary>
    public override Walk? Walk => null;

    /// <summary>Three numbers the film cannot be shown without, taken from it rather than repeated. A
    /// near plane of five centimetres in a scene two and a half million units across would spend the
    /// whole depth buffer on the first metre of it.</summary>
    public override float Lens => ContactScene.Lens;

    public override float Near => ContactScene.Near;

    public override float Far => ContactScene.Far;

    /// <summary>
    /// The world outside, and the four lights on it.
    ///
    /// This is the one hand-over in the film where every slot changes at once, and it is the only one
    /// that could not have been done gradually: the building's lamps are metre-scale things at intensity
    /// one and these are a star, a planet's bounce and two ranged lights quoted in candela at station
    /// scale. There is no crossfade between those two rigs that is not a mess, which is a second argument
    /// for the cut being a cut.
    ///
    /// The room is occupied alone. Everything the building has — seven rooms, four doors, the engine
    /// room's fan — goes out of the scene on this frame, which is what pays for a sky sphere two and a
    /// half million units across arriving on it.
    /// </summary>
    public override void Enter(Hall hall)
    {
        hall.Occupy(Deck.ContactRoom);
        hall.Use(film.Lights);
        hall.Ambient(film.Sky);
    }

    /// <summary>
    /// The film, wound to this second of itself.
    ///
    /// It is the one chapter in the story that is not a pure function of its clock, and the honest thing
    /// is to say exactly how far that goes. Every ship's position, attitude and roll is a closed form in
    /// the time through the cycle, so seeking here puts the whole convoy, both escorts and all three
    /// raiders precisely where they belong. What is not closed-form is the tracers and the explosions:
    /// they are particles with velocities, so jumping into the middle of the engagement arrives at a
    /// battle whose rounds are still catching up, and for about a second there is less in the air than
    /// there should be.
    ///
    /// That is worth having rather than worth fixing. The alternative is a battle whose every round is a
    /// function of the clock, which is a battle where nothing can miss.
    /// </summary>
    public override void Update(Hall hall, float seconds)
    {
        film.Advance(seconds);
        hall.Scene.Invalidate();
    }

    /// <summary>The shot list, which is the film's own and is why this chapter has no walk — and then the
    /// black over the last three seconds of it.</summary>
    public override void Shoot(Camera camera, float seconds)
    {
        film.Aim(camera);
        curtain.Draw(camera, Ramp(seconds, Duration - Dusk, Dusk), 0f);
    }

    /// <summary>Whatever the shot that is running says. Set inside <see cref="Shoot"/>, which the story
    /// runs before it reads a caption — the same order the scene uses on its own.</summary>
    public override string? Caption(float seconds) => film.Caption;
}
