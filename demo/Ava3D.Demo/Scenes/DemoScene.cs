using System;
using System.Numerics;
using Ava3D;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// One thing the control can do, built from nothing.
///
/// Every scene in this folder is a single file with no dependencies on the others, so it can be read on
/// its own and copied into a real application without untangling it from the demo shell. That is the point
/// of the shape: the interesting part of a demo is the twenty lines that build the scene, and those lines
/// should not be buried in a switch statement. Two folders are exceptions and both say so at the top of
/// themselves: <c>Contact/</c> is a sixty-second film rather than a demonstration of anything in
/// particular, and <c>Board/</c> is one model looked at four ways, where the loading and the manifest are
/// identical in all four and copying them would only make three of them go stale.
///
/// A scene is built once when it is selected and then, if it animates, nudged each frame. It keeps
/// references to whatever nodes it wants to move in its own fields — which is why these are classes rather
/// than functions.
/// </summary>
public abstract class DemoScene
{
    /// <summary>Short name, for the list.</summary>
    public abstract string Title { get; }

    /// <summary>One line, for the list.</summary>
    public abstract string Summary { get; }

    /// <summary>
    /// What this scene is actually showing, in a couple of sentences. Displayed beside the viewport,
    /// because a demo that does not say what it is demonstrating is just a picture.
    /// </summary>
    public abstract string Notes { get; }

    /// <summary>
    /// Builds the scene as it is seen on its own: the subject, standing on a stage.
    ///
    /// Called once per selection; store node references for <see cref="Update"/>.
    ///
    /// Every scene in this folder now uses this default, and the only overrides left are the four in
    /// <c>Board/</c>, which tolerate a subject that failed to load rather than building anything
    /// differently. The throw is therefore not defensive about the split any more — it is the answer to a
    /// new scene that implements neither half, which would otherwise draw an empty grey card and look
    /// like a rendering bug rather than an unfinished file.
    /// </summary>
    public virtual Scene Build()
    {
        var scene = new Scene();
        Stage(scene);
        scene.Children.Add(BuildSubject() ?? throw new InvalidOperationException(
            $"{GetType().Name} overrides neither Build nor BuildSubject — one of them has to make something"));

        return scene;
    }

    /// <summary>
    /// The subject alone: the nodes this scene exists to show, with no ground under them, no backdrop
    /// behind them and no lights on them.
    ///
    /// This is the half of a scene that can be picked up and put somewhere else, and it is what the story
    /// mounts — on a plinth, in a niche, on a bench. Every scene here has one; there is no longer such a
    /// thing as a scene the story cannot mount.
    ///
    /// Null means there is nothing to show at all, which in practice means geometry that would not load,
    /// and a scene that can say it has to override <see cref="Build"/> to tolerate it. Only the four board
    /// scenes do. The other three that can fail the same way return a holder instead and never say null —
    /// the camera and the motherboard leave it empty, and the round-trip glb puts a red box in it, because
    /// its failure is a bug in this demo rather than a missing file and should look like one. All of them
    /// say what happened in <see cref="Notes"/>. Prefer the holder: it needs no override.
    ///
    /// Lights are deliberately not part of it. A light lives on the <see cref="Scene"/> rather than in the
    /// node tree, there are four slots in the whole scene, and a room that is already spending them cannot
    /// take an exhibit's as well — see <see cref="Stage"/> for where a scene's own lighting goes.
    /// </summary>
    public virtual Node? BuildSubject() => null;

    /// <summary>
    /// Everything around the subject that makes it readable on its own: the backdrop, the floor, the
    /// lights, the environment.
    ///
    /// Called only when the scene is shown by itself. The story never calls it, because in the story the
    /// room is the stage — its floor, its lamps, its walls. A scene whose subject is the lighting rather
    /// than the object overrides this to arrange the lights it is demonstrating, and is mounted as a room
    /// rather than as an exhibit.
    ///
    /// The default is currently unreached, and that is not a mistake to be tidied away. Every scene in this
    /// folder overrides it, because a starfield and a lit still life do not want the same floor and there
    /// is no honest average of the two — which is the finding, and it is the opposite of what was expected.
    /// <see cref="Staging.Neutral"/> stays because it is still the right first answer for a scene nobody
    /// has written yet: something to stand on and something to see it by, so a new file draws a picture
    /// before its author has decided anything.
    /// </summary>
    public virtual void Stage(Scene scene) => Staging.Neutral(scene);

    /// <summary>
    /// Aims the camera, for scenes that only read correctly from one angle — a chart wants to be seen
    /// square-on, not from three-quarters.
    ///
    /// Set <see cref="Camera.Yaw"/>, <see cref="Camera.Pitch"/> and field of view here and leave
    /// <see cref="Camera.Target"/> and <see cref="Camera.Distance"/> alone: AutoFit sets those from the
    /// scene's extent afterwards, so a scene that only states its angle still gets framed properly.
    /// </summary>
    public virtual void Frame(Camera camera) { }

    /// <summary>
    /// The scene is being taken off. Let go of anything that outlives a <see cref="Scene"/>.
    ///
    /// Nearly nothing needs this, which is why it is a virtual with an empty body rather than
    /// <see cref="IDisposable"/> on every scene in the folder: a scene is meshes and materials, the picker
    /// drops its instance, and the collector takes the lot. What it exists for is the one kind of thing a
    /// garbage collector cannot help with — a resource that is still running when nobody is holding it.
    /// The film's soundtrack is that: an audio device with a thread of its own, which would go on playing
    /// a room tone from a building that is no longer on screen.
    /// </summary>
    public virtual void Retire() { }

    /// <summary>
    /// Whether <see cref="Frame"/> sets the whole camera, target and distance included, and AutoFit should
    /// therefore stay out of the way.
    ///
    /// AutoFit is the right default — it frames an arbitrary scene without being told anything about it —
    /// but it frames a bounding sphere with a margin, which leaves a chart of spheres smaller on screen than
    /// a chart of spheres wants to be.
    /// </summary>
    public virtual bool FramesItself => false;

    /// <summary>Whether this scene wants <see cref="Update(Scene, double)"/> called.</summary>
    public virtual bool Animates => false;

    /// <param name="scene">The scene returned by <see cref="Build"/>.</param>
    /// <param name="elapsed">Seconds since the scene was selected.</param>
    public virtual void Update(Scene scene, double elapsed) { }

    /// <summary>
    /// The per-frame hook for a scene that moves the camera as well as the scene — a scripted shot,
    /// a chase cam, anything that is a film rather than a turntable.
    ///
    /// It defaults to the two-argument overload, so a scene that only animates its own nodes overrides
    /// that one and never sees this. Override this one instead and set <see cref="DrivesCamera"/>.
    /// </summary>
    public virtual void Update(Scene scene, Camera camera, double elapsed) => Update(scene, elapsed);

    /// <summary>
    /// Whether the camera is scene-driven. The shell pushes the camera to the renderer every frame when
    /// it is, and leaves it entirely to mouse input when it is not — which is the right default, and one
    /// frame of work per frame that a static camera should not pay for.
    /// </summary>
    public virtual bool DrivesCamera => false;

    /// <summary>
    /// Whether the viewer has the controls right now: mouse to look, keys and the on-screen buttons to walk.
    ///
    /// Polled every frame rather than read once, because a scene can hand over partway through — the film
    /// does exactly that, at the end, having spent nine minutes walking you around the building itself.
    /// The shell answers it by turning the view's own orbit on and by showing its two step buttons; a scene
    /// that says yes must also still say yes to <see cref="DrivesCamera"/>, because it is now placing the
    /// camera from the viewer's input instead of from a script, which is still placing it.
    /// </summary>
    public virtual bool WantsControl => false;

    /// <summary>
    /// How the viewer is asking to move, in the camera's own frame: X is sideways, Y is forwards, each in
    /// −1 to 1. Pushed by the shell whenever it changes rather than polled.
    ///
    /// It is a direction and not a distance on purpose. How fast a metre goes by is the scene's business —
    /// it knows whether it is a person on a deck or a ship in open space — and the shell only knows which
    /// keys are down.
    /// </summary>
    public virtual void Steer(Vector2 move) { }

    /// <summary>
    /// A line of text over the bottom of the viewport, polled every frame. Null for no caption.
    ///
    /// This is for a scene that is telling a story and needs to say where you are in it. A scene whose
    /// explanation does not change belongs in <see cref="Notes"/>, which is laid out to be read.
    /// </summary>
    public virtual string? Caption => null;

    /// <summary>
    /// How long Auto holds on this scene before moving to the next one.
    ///
    /// Nine seconds is long enough to watch a turntable go round, which is what most of these are. A
    /// scene with a script sets this to the length of the script instead, so Auto does not advance on a
    /// timeout at all — it advances when the scene is over. Contact runs sixty seconds and asks for
    /// sixty-two.
    /// </summary>
    public virtual TimeSpan TourDuration => TimeSpan.FromSeconds(9);

    /// <summary>
    /// Whether clicking should highlight what was hit. Off for most scenes so a stray click during the
    /// automatic tour does not leave a recoloured object behind.
    /// </summary>
    public virtual bool WantsPicking => false;

    /// <summary>
    /// The click, offered to the scene before the shell does anything with it. Return true to say the
    /// scene has dealt with it and the shell should keep its hands off.
    /// </summary>
    /// <param name="scene">The scene returned by <see cref="Build"/>.</param>
    /// <param name="hit">What the ray found, or null for a click on nothing.</param>
    /// <remarks>
    /// The shell's own answer to a click — clone the material, tint it orange, name the node — is the
    /// right one for a scene of unrelated objects, and it is the wrong one as soon as the thing a person
    /// clicked is not the thing they meant. On the board, one component is several nodes: clicking a
    /// memory slot hits <c>dimm.1.latch</c>, and tinting that alone highlights a plastic clip while
    /// leaving the slot it belongs to untouched. What the scene knows and the shell cannot is which
    /// nodes make up a part, what the part is called, and which copper leaves it.
    ///
    /// So this is a scene taking over, not a notification: a scene that returns true owns the highlight
    /// and everything shown about it, which is why <see cref="Caption"/> is where the answer goes.
    /// </remarks>
    public virtual bool Picked(Scene scene, PickResult? hit) => false;

    /// <summary>
    /// How the notes panel should dress itself for this scene. See <see cref="SceneLook"/>.
    /// </summary>
    public virtual SceneLook Look => SceneLook.Plain;
}

/// <summary>
/// The four dresses the notes overlay wears.
///
/// One style for every scene made all twenty-six of them look like the film, which is the only one that
/// earned the look. A scene about back-face culling is a diagram; a scene about roughness is a lit still
/// life; Hello cube is a manual page. Matching the frame to what is inside it costs one property and
/// stops the demo reading as though every subject were the same subject.
///
/// Four rather than twenty-six on purpose: a look that is unique to one scene is decoration, and a look
/// shared by a group is a statement that the group belongs together. These four are the groups the
/// catalogue is already ordered by.
/// </summary>
public enum SceneLook
{
    /// <summary>Neutral grey card. The fundamentals, and the ones that are about the control rather than
    /// about shading — picking, animation, the stress test.</summary>
    Plain,

    /// <summary>Drawing-office blue with a visible edge. The scenes that are a comparison or a diagram:
    /// culling, blending, lines, points, sprites, and the two kinds of surface detail.</summary>
    Blueprint,

    /// <summary>Warm neutral. Materials, lighting, textures, models and the PBR scenes — everything whose
    /// subject is a lit object.</summary>
    Studio,

    /// <summary>Near-black with a cold green title. The two scenes that are in space.</summary>
    Cosmic
}
