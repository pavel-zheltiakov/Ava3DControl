using System;
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

    /// <summary>Builds the scene. Called once per selection; store node references for <see cref="Update"/>.</summary>
    public abstract Scene Build();

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
