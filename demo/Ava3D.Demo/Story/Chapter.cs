namespace Ava3D.Demo.Story;

/// <summary>
/// One chapter of the film: a room, a walk through it, and whatever the room does while he is in it.
///
/// A chapter is not a scene. It builds nothing — the rooms are already standing and the exhibits are
/// already mounted — it says which room exists right now, which four lights are spending the slots, and
/// where the visitor is at every second of it. That is the whole interface, and it is small on purpose:
/// everything expensive happens once, in <see cref="Film"/>, and a chapter change is a handful of
/// booleans and a camera.
///
/// <see cref="Enter"/> and <see cref="Update"/> are split along the line between what is true for the
/// whole chapter and what changes during it. The split matters because of seeking: jumping to the middle
/// of a chapter runs <see cref="Enter"/> once and then <see cref="Update"/> at the target second, and
/// anything that only happened in the first frame of the chapter would be missing. So <see cref="Update"/>
/// has to be able to produce any moment from the time alone — the same rule the walk follows, for the same
/// reason.
/// </summary>
internal abstract class Chapter
{
    /// <summary>Short name, for the contents.</summary>
    public abstract string Title { get; }

    /// <summary>How long it runs before the next chapter begins.</summary>
    public abstract float Duration { get; }

    /// <summary>
    /// Where the visitor is, second by second — and null in a chapter where he is not anywhere.
    ///
    /// There is one of those, and it is the cut. For eight chapters the film is a man walking around a
    /// ship and the camera is his eye; for the ninth it is a film about that ship, with a shot list and a
    /// camera that flies itself, and the whole force of the cut is that he has stopped being the camera.
    /// So the null is not a missing walk, it is the statement — see <see cref="Shoot"/>, which is what a
    /// chapter without one has instead.
    ///
    /// It is also the one flag anything else needs. A chapter with a walk is a chapter where the visitor
    /// is a body standing on a deck, which is exactly the question <c>Ground.Audit</c> asks before it
    /// tests a chapter for going through walls, and exactly the question the film asks at the end before
    /// it hands the building over.
    /// </summary>
    public virtual Walk? Walk => null;

    /// <summary>
    /// Puts the camera where this second of the chapter wants it.
    ///
    /// The default is the walk, and it is what eight of the nine chapters use. Roll is reset with it
    /// because a person's horizon is level and because roll is state that survives a cut: the chapter
    /// that flies leaves the camera banked, and without this the first walking chapter after it would
    /// inherit the tilt.
    /// </summary>
    public virtual void Shoot(Camera camera, float seconds)
    {
        var pose = (Walk ?? throw new InvalidOperationException(
            $"{Title} has neither a walk nor a camera of its own — one of them has to aim")).At(seconds);

        camera.Roll = 0f;
        camera.LookFrom(pose.Eye, pose.Look);
    }

    /// <summary>
    /// The lens, in degrees of vertical field of view.
    ///
    /// Fifty-five is a person's eye. It is a little wide for a film camera and about right for standing
    /// in a small room: at forty-five the antechamber reads as a corridor.
    /// </summary>
    public virtual float Lens => 55f;

    /// <summary>
    /// How close this chapter can see, in metres. Five centimetres is a face against a wall.
    ///
    /// Unlike <see cref="Far"/> this one is expensive to move, and that asymmetry is the whole reason
    /// they are two properties. Depth resolution at a given distance goes as that distance squared over
    /// the near plane, so pushing the near plane out by a factor of a hundred buys a hundred times the
    /// precision everywhere — which is what the chapter in open space needs and what a chapter with a
    /// doorframe half a metre from the lens must never be given.
    /// </summary>
    public virtual float Near => 0.05f;

    /// <summary>
    /// How far this chapter can see, in metres.
    ///
    /// Two hundred and twenty for the whole building, which is generous for rooms fourteen metres across
    /// and is there so a doorway never clips the room behind it. One chapter needs a great deal more —
    /// see <see cref="Outside"/>, which has a starfield fourteen hundred metres out — and it is a property
    /// rather than a constant because the cost is real and belongs to the chapter that incurs it.
    ///
    /// The near plane is deliberately not paired with it. Depth resolution at a given distance goes as
    /// that distance squared over the near plane, and the far plane barely enters into it, so moving this
    /// alone does not put the walls of the building into a depth fight.
    /// </summary>
    public virtual float Far => 220f;

    /// <summary>
    /// Puts the building into the state this chapter starts in: which rooms exist, which lights are on.
    /// Called once when the chapter is entered, including when it is jumped into.
    /// </summary>
    public abstract void Enter(Hall hall);

    /// <summary>
    /// What the room does at <paramref name="seconds"/> into the chapter. Must be a pure function of the
    /// time — see the class remarks.
    /// </summary>
    public virtual void Update(Hall hall, float seconds) { }

    /// <summary>A line over the bottom of the frame, or null.</summary>
    public virtual string? Caption(float seconds) => null;

    /// <summary>
    /// Fade in, hold, fade out — the shape a cue that is brought up and taken away again has.
    ///
    /// Here rather than in each chapter because half of them need it: a lamp coming up, a screen waking, a
    /// bank of light handing over at a threshold. Returns 0 before <paramref name="from"/>, 1 after
    /// <paramref name="from"/> plus <paramref name="over"/>, and a smoothstep between.
    /// </summary>
    protected static float Ramp(float seconds, float from, float over)
    {
        if (over <= 0f)
            return seconds >= from ? 1f : 0f;

        var u = Math.Clamp((seconds - from) / over, 0f, 1f);
        return u * u * (3f - 2f * u);
    }
}
