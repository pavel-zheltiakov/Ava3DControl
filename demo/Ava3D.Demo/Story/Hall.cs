using System.Numerics;
using Avalonia.Media;

namespace Ava3D.Demo.Story;

/// <summary>
/// The building: a set of rooms in one scene, of which one or two exist at a time.
///
/// The rooms are all built once and then hidden, rather than built and torn down as the visitor moves.
/// Building them once is what makes the place continuous — a room rebuilt at a threshold can differ from
/// the room that was there a moment ago, and the difference reads as the world jumping — and hiding them
/// costs nothing, because <see cref="Scene.EnumerateMeshes"/> stops at an invisible node and never walks
/// what is behind it.
///
/// That is also the whole of the visibility scheme, and it is enough. A general portal system solves the
/// problem of not knowing which cells can see which; here there is exactly one route through seven rooms,
/// so the answer is always "the room he is in, and the one he can see through the door" — which the
/// chapter already knows, because knowing where he is is what a chapter is for.
///
/// <see cref="Use"/> is the other half. Four lights light the whole building, one room needs about four,
/// and the arithmetic only works because a room hands its lights over at the threshold rather than adding
/// to them. Four is the film's number and not the renderer's: it was the cap when the building was drawn,
/// <see cref="LightCollection.Capacity"/> is sixteen now, and it is kept because every room in here was
/// composed against it. Chapter 7 is the proof: with every lamp in the building switched off, the picture
/// still has the screens and the exit strips in it, which is only possible because nothing accumulated.
/// </summary>
internal sealed class Hall
{
    private readonly Dictionary<string, Node> _rooms = new(StringComparer.Ordinal);

    /// <summary>The two-colour hemisphere, kept so it can be written to rather than rebuilt. Null until the
    /// first chapter asks for one, and dropped whenever a room puts a baked probe on the scene instead.</summary>
    private EnvironmentLight? _hemisphere;

    public Hall()
    {
        Scene = new Scene { Background = Color.FromRgb(3, 3, 4) };

        // A scene arrives with one directional light in it, which is the right default for a scene that is
        // outdoors or is a still life and the wrong one for the inside of a building. Every light in here
        // is a lamp somebody could point at.
        Scene.Lights.Clear();
        Scene.Environment = EnvironmentLight.None;
    }

    public Scene Scene { get; }

    /// <summary>Adds a room at a place on the deck and returns its root, for the room builder to fill.</summary>
    public Node Add(string name, Vector3 origin)
    {
        // A third of a millimetre per room, and it is the only fix in this file that is a number rather
        // than a rule.
        //
        // Every room in the building has a floor at y = 0 and walls that stand on it, and the plan is drawn
        // so that rooms <i>meet</i> — the passage runs up to the rotunda's wall, the corridor's end stands
        // inside the engine room's bulkhead. Where two of them overlap, two floors are coplanar, and two
        // coplanar surfaces are the one case a depth buffer cannot decide: what it looks like is a grout
        // line that goes dashed and a tile that tears, changing every time the camera moves. It has been in
        // the film since the second room and it was nearly invisible there, because a chapter only ever put
        // two rooms up and the walk never stood on the join. The free walk stands on every join in the
        // building.
        //
        // Lifting each room by its own hair means no two floors, no two ceilings and no two wall faces in
        // the building are ever level with each other. Eight rooms is 2.4 mm from end to end, which is a
        // step nobody can see, nothing can trip on — see Ground.Knee, which steps over 36 cm — and which
        // the collision reads as a plan view anyway.
        var root = new Node
        {
            Name = name,
            Position = origin + new Vector3(0f, _rooms.Count * 0.0003f, 0f),
            IsVisible = false
        };

        _rooms.Add(name, root);
        Scene.Children.Add(root);

        return root;
    }

    /// <summary>A room by name. Throws rather than returning null: a chapter naming a room that is not
    /// there is a typo, and a typo should not read as an empty building.</summary>
    public Node Room(string name) => _rooms.TryGetValue(name, out var room)
        ? room
        : throw new KeyNotFoundException($"no room called '{name}' — the film names it but the hall has not built it");

    /// <summary>
    /// Which rooms exist right now. Everything not named is hidden.
    /// </summary>
    public void Occupy(params string[] names)
    {
        foreach (var (name, root) in _rooms)
            root.IsVisible = Array.IndexOf(names, name) >= 0;

        Scene.Invalidate();
    }

    /// <summary>
    /// Hands the building's lights to these. Anything previously lighting it stops.
    /// </summary>
    /// <exception cref="InvalidOperationException">More lights than <see cref="LightCollection.Capacity"/>,
    /// which is the most every renderer will draw rather than the most any of them will. Past it a room
    /// would look like one room here and another one in a browser tab, and a film that is not the same
    /// film everywhere is worse than a film that refuses to start.</exception>
    public void Use(params Light[] lights)
    {
        if (lights.Length > LightCollection.Capacity)
            throw new InvalidOperationException(
                $"a room asked for {lights.Length} lights and {LightCollection.Capacity} is what every " +
                "renderer will draw — spend fewer, or bake them");

        Scene.Lights.Clear();

        foreach (var light in lights)
            Scene.Lights.Add(light);

        Scene.Invalidate();
    }

    /// <summary>
    /// How much light there is in the building that is not coming from a lamp — the bounce off the walls
    /// that a forward renderer has no other way to account for, because a point light does not bounce
    /// however many of them there are.
    ///
    /// Kept low. Raised far enough to be comfortable it flattens the rooms into cardboard, and the whole
    /// look of the place is that the light has somewhere it is coming from.
    /// </summary>
    public void Ambient(float sky, float ground)
    {
        // Mutated rather than replaced, and it matters more than it looks. Chapter 4 takes the ambient down
        // with its lamps, on every frame for twenty seconds, and a new EnvironmentLight per frame is a small
        // allocation the collector has to deal with on the thread that is drawing.
        //
        // The one this reuses has to be one this method made, and that is not fussiness. The first version
        // reused whatever was on the scene as long as it carried no texture — which on the first call is
        // EnvironmentLight.None, whose Intensity is zero. Every colour it was then handed was multiplied by
        // nothing, so the whole building rendered with no bounce light at all, in every room, for the whole
        // film. It was very nearly invisible, because a room lit by four lamps at full is still a lit room;
        // what gave it away was the one frame in the film with no lamps on in it, which came out as literal
        // black rather than as a dark alcove with a ball hanging in it.
        if (!ReferenceEquals(Scene.Environment, _hemisphere))
        {
            _hemisphere = new EnvironmentLight { Intensity = 1f };
            Scene.Environment = _hemisphere;
        }

        _hemisphere!.SkyColor = new Vector3(sky * 0.9f, sky * 0.94f, sky);
        _hemisphere.GroundColor = new Vector3(ground, ground * 0.94f, ground * 0.86f);
    }

    /// <summary>
    /// The air between the camera and what it is looking at, and the stop the camera is set to.
    ///
    /// <b>One room in the building has any, and that is the point of it being a method.</b> Every
    /// exhibition room here is six to fourteen metres across, and fog over that distance is a tint on the
    /// far wall — the argument for it is depth, and there is no depth to argue about. The alarm corridor is
    /// twenty-one metres of two-metre passage with seven turning beacons down it, which is the one place in
    /// the film where "how far away is that" is a question the picture has to answer.
    ///
    /// <paramref name="colour"/> is the colour distant geometry <i>ends up</i>, not a linear value to be
    /// tone mapped — see <see cref="Scene.FogColor"/>, which mixes after the tone map for exactly that
    /// reason. In a dark corridor that is very nearly black, and the amber in it is the beacons.
    ///
    /// The exposure comes with it because in that corridor the two are one decision. He walks out of a lit
    /// lounge into a dark passage, and a camera that did not stop down would be a camera that is not an
    /// eye.
    /// </summary>
    public void Air(float from, float to, Vector3 colour, float exposure = 1f)
    {
        Scene.FogStart = from;
        Scene.FogEnd = to;
        Scene.FogColor = colour;
        Scene.Exposure = exposure;

        Scene.Invalidate();
    }

    /// <summary>
    /// Clear air and an open stop, which is what every room but one has.
    ///
    /// Called by <see cref="StoryScene"/> on every chapter change rather than by each chapter, and that is
    /// the one place in this building where the film asserts something on a chapter's behalf. The reason is
    /// the rule the chapters already follow: a chapter's <c>Enter</c> puts the building into a state rather
    /// than assuming the one before it left it there — and fog is a property of the <i>scene</i> rather
    /// than of a room, so honouring that rule per chapter would mean eleven chapters each saying they have
    /// no fog. One of them would be forgotten, and what that looks like is the engine room seen through the
    /// corridor's air.
    /// </summary>
    public void Clear() => Air(0f, 0f, Vector3.Zero);

    /// <summary>
    /// The same thing with an image instead of two colours: a baked probe of the room he is in.
    ///
    /// One room needs it and the rest do not, which is the whole reason this is a second method rather than
    /// the only one. Plaster reflects nothing and a hemisphere of sky and ground is a perfectly good account
    /// of the bounce in a room full of it. A metal reflects only the environment, so in a room with metals
    /// in it the environment stops being an ambient term and becomes the exhibit — see
    /// <see cref="Gallery"/>, which bakes its own.
    ///
    /// The <see cref="EnvironmentLight"/> is passed in already built rather than made here, because setting
    /// one with a texture prefilters the image into its blurred copies, and a chapter that can be seeked
    /// into would pay for that on every jump.
    /// </summary>
    public void Ambient(EnvironmentLight probe)
    {
        Scene.Environment = probe;
        Scene.Invalidate();
    }
}
