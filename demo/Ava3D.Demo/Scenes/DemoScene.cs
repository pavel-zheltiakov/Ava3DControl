using Ava3D;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// One thing the control can do, built from nothing.
///
/// Every scene in this folder is a single file with no dependencies on the others, so it can be read on
/// its own and copied into a real application without untangling it from the demo shell. That is the point
/// of the shape: the interesting part of a demo is the twenty lines that build the scene, and those lines
/// should not be buried in a switch statement.
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

    /// <summary>Whether this scene wants <see cref="Update"/> called.</summary>
    public virtual bool Animates => false;

    /// <param name="scene">The scene returned by <see cref="Build"/>.</param>
    /// <param name="elapsed">Seconds since the scene was selected.</param>
    public virtual void Update(Scene scene, double elapsed) { }

    /// <summary>
    /// Whether clicking should highlight what was hit. Off for most scenes so a stray click during the
    /// automatic tour does not leave a recoloured object behind.
    /// </summary>
    public virtual bool WantsPicking => false;
}
