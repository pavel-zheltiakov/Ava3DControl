using System.Numerics;
using Ava3D.Demo.Views;

namespace Ava3D.Demo.Story;

/// <summary>
/// The one piece of film grammar in the exhibition: a fade, and a card with two words on it.
///
/// Everything else in these ten chapters is a thing in the world. The lights are lamps somebody could
/// point at, the captions are a man's handover note, and the single cut is earned by nine minutes of not
/// taking one. This is not — it is a black frame, and nothing on a ship makes a black frame. It is here
/// because the film asks the audience to skip a night between the convoy docking and the morning it
/// leaves, and there is no diegetic way to say <i>a day passed</i> to somebody who has been standing in
/// one continuous shot since the dark.
///
/// <b>It is geometry, not an overlay.</b> A quad and a hundred and thirty line segments, held a finger's
/// width in front of the lens and turned to face it, rather than something drawn on top of the picture by
/// the view. Three reasons, in order of how much they matter. A frame grab of this demo is taken by the
/// renderer — see <c>AVA3D_CAPTURE</c> — and cannot see an overlay at all, so a title card drawn there is
/// a title card nobody can check. The four heads this demo runs on draw their overlays through four
/// different stacks and the scene through one. And the fade has to be on the same clock as the picture it
/// is fading, which is a guarantee a control that ticks itself cannot make.
///
/// <b>The face is the caption band's</b> — see <see cref="Glyphs"/>, forty-nine glyphs on a five-by-seven
/// grid, drawn rather than licensed. That is not an economy here so much as the only consistent answer:
/// the words over the black are the same words the panel would have set, in the same hand.
///
/// It belongs to no room. <see cref="Hall.Occupy"/> hides everything that is not the room he is in, and
/// this hangs off the scene itself, because the two chapters that use it are in two different rooms and
/// one of them is a planet. What keeps it from being left up is <see cref="Clear"/>, which the film calls
/// on every frame before the chapter aims — so a curtain is down only while somebody is actively holding
/// it there, and seeking out of chapter 8 cannot leave the screen black.
/// </summary>
internal sealed class Curtain
{
    /// <summary>
    /// How far in front of the lens the veil hangs, as a multiple of the near plane.
    ///
    /// A multiple rather than a distance, because the two chapters that use it are at scales six orders of
    /// magnitude apart: the morning's near plane is five centimetres and is set by a doorframe, and the
    /// cut's is five metres and is set by a battle three thousand of them across. Anything nearer than the
    /// near plane is not drawn, and anything much further is something the world can get in front of.
    /// </summary>
    private const float Standoff = 2.4f;

    /// <summary>How much wider than tall the veil is cut. The frame's height is known — it is the field of
    /// view — and its width is not, because that is the window's business and changes when somebody drags
    /// a corner. Six to one covers every shape a screen has ever been, and the cost of the margin is nought
    /// pixels: what is off the frame is clipped before it is shaded.</summary>
    private const float Wider = 6f;

    /// <summary>Cap height of the card, as a fraction of the frame's height. A tenth is a title; a fifth is
    /// a warning.</summary>
    private const float Lettering = 0.098f;

    private readonly Node _root = new() { Name = "curtain", IsVisible = false };
    private readonly Material _skin;
    private readonly MeshNode _veil;
    private readonly LineNode _words;

    /// <param name="hall">The building, for the scene the curtain hangs off.</param>
    /// <param name="title">What the card says. Set once — a card whose words change is a subtitle.</param>
    public Curtain(Hall hall, string title)
    {
        hall.Scene.Children.Add(_root);

        _skin = new Material
        {
            BaseColor = Vector4.Zero,
            Unlit = true,
            Blend = BlendMode.Alpha,

            // Neither tested nor written. It is in front of the near plane's own business and it is not
            // part of the world: a curtain that wrote depth would leave a hole in the frame after it, and
            // one that was tested could be argued with by a wall it is supposed to be hiding.
            DepthTest = false,
            DepthWrite = false,
            Cull = CullMode.None,
            Name = "curtain.veil"
        };

        // A plane lies in XZ, which is right for a floor and wrong for a screen. Turned a quarter about X
        // it lies in XY, which is the plane the node's own rotation puts across the lens.
        _veil = new MeshNode(
            Primitives.Plane().Transformed(Matrix4x4.CreateRotationX(MathF.PI / 2f)), _skin)
        {
            IsPickable = false,

            // Last of everything, in a film whose largest render order is the docking gate's. Nothing is
            // allowed over the top of a fade — that is what makes it a fade rather than a filter.
            RenderOrder = 5_000,
            Name = "veil"
        };

        _root.Children.Add(_veil);

        _words = new LineNode
        {
            Positions = Card(title),
            Color = new Vector3(0.86f, 0.90f, 1f),
            Width = 3f,
            Blend = BlendMode.Additive,
            DepthTest = false,
            DepthWrite = false,
            RenderOrder = 5_001,
            IsVisible = false,
            Name = "title"
        };

        _root.Children.Add(_words);
    }

    /// <summary>
    /// Nothing over the picture. Called by the film every frame before the chapter aims, so holding the
    /// curtain down is something a chapter has to do continuously rather than something it switches on.
    /// </summary>
    public void Clear() => _root.IsVisible = false;

    /// <summary>
    /// Puts the curtain across whatever this camera is looking at.
    ///
    /// Both levels are separate on purpose. The cut fades to black with no card on it, the morning opens
    /// on a card over black and then takes the black away from under it, and the two moments meet at a
    /// chapter boundary where both are exactly one — which is what makes the join invisible rather than a
    /// second cut.
    /// </summary>
    /// <param name="camera">Where the picture is being taken from, already aimed.</param>
    /// <param name="black">How opaque the veil is.</param>
    /// <param name="words">How bright the card is.</param>
    public void Draw(Camera camera, float black, float words)
    {
        black = Math.Clamp(black, 0f, 1f);
        words = Math.Clamp(words, 0f, 1f);

        _root.IsVisible = black > 0.002f || words > 0.002f;

        if (!_root.IsVisible)
            return;

        var at = (camera.NearPlane ?? 0.05f) * Standoff;

        // How tall the frame is where the veil hangs. The field of view is the vertical one — see
        // Chapter.Lens — so this is the whole of the arithmetic and the width is a margin rather than a
        // measurement.
        var height = 2f * at * MathF.Tan(camera.FieldOfView * 0.5f * MathF.PI / 180f);

        var forward = camera.Forward;
        var up = camera.Up;
        var right = camera.Right;

        _root.Position = camera.Position + forward * at;

        // The camera's own basis, written as a rotation: local X is screen right, local Y is screen up, and
        // local Z is out of the screen — which is minus the way the camera is looking. Screen right rather
        // than the deck's right, deliberately: the card has to stay level with the frame, not with the
        // floor, and this is the one object in the film for which those are different.
        _root.Rotation = Quaternion.CreateFromRotationMatrix(new Matrix4x4(
            right.X, right.Y, right.Z, 0f,
            up.X, up.Y, up.Z, 0f,
            -forward.X, -forward.Y, -forward.Z, 0f,
            0f, 0f, 0f, 1f));

        _veil.Scale = new Vector3(height * Wider, height, 1f);
        _skin.BaseColor = new Vector4(0f, 0f, 0f, black);

        _words.Scale = new Vector3(height * Lettering);
        _words.Opacity = words;
        _words.IsVisible = words > 0.002f;
    }

    /// <summary>
    /// The title, as segment pairs about its own centre, one unit of cap height tall.
    ///
    /// <see cref="Glyphs.Segments"/> hands back a word laid out left to right from the origin with y
    /// running <i>down</i> the cell, which is what a canvas wants and is upside down in a scene. So this
    /// flips it, divides by the cap height so a caller can ask for a size in metres, and slides it half its
    /// own width left — a card that is not centred is a card somebody forgot to centre.
    /// </summary>
    private static Vector3[] Card(string title)
    {
        var flat = Glyphs.Segments(title);
        var wide = (float)Glyphs.Measure(title, Glyphs.CellHeight);

        var card = new Vector3[flat.Length];

        for (var i = 0; i < flat.Length; i++)
            card[i] = new Vector3(
                (flat[i].X - wide / 2f) / Glyphs.CellHeight,
                (Glyphs.CellHeight / 2f - flat[i].Y) / Glyphs.CellHeight,
                0f);

        return card;
    }
}
