using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// A shadow with its answer drawn next to it.
///
/// Every other scene here shows what the renderer does. This one shows what it <i>should</i> do at the same
/// time, in the same picture, so the two can be compared by looking rather than by remembering. One flat
/// caster hangs over one flat floor under one point light. The yellow lines are the light's own rays,
/// traced from the bulb through the caster's silhouette and on until they reach the floor; the red crosses
/// are where they land; the cyan loop through those crosses is the shadow's outline as geometry demands it.
/// If the grey shadow the shader painted does not sit inside that loop, the shader is wrong, and by how much
/// is readable off the floor in world units.
///
/// The arithmetic is one line and it is exact for a point light. A ray from the bulb at <c>L</c> through a
/// silhouette vertex <c>P</c> reaches the floor at <c>L + (P − L) · L.y / (L.y − P.y)</c>, so a flat quad —
/// four corners — has a four-cornered shadow, and a flat circle has a conic. Nothing here is a second
/// renderer or a ray tracer that could be wrong in its own way: both sides are closed form, which is the
/// whole reason this scene can decide anything.
///
/// Four shapes, in two pairs. <c>quad</c> and <c>circle</c> are the flat plates, with no thickness at all.
/// <c>slab</c> and <c>puck</c> are solids a few centimetres thick with exactly the same silhouette, so the
/// predicted outline is the same to within that thickness and any difference between a pair is the depth
/// pass treating an open shell differently from a closed one. It does: it keeps only faces turned away from
/// the light, and a single plane facing the light has none. Watch a quad rotate through 360° and its shadow
/// appears for half the turn.
/// </summary>
public sealed class ShadowProbeScene : DemoScene
{
    /// <summary>The receiver's height. Everything here assumes the floor is the plane y = 0.</summary>
    private const float FloorY = 0f;

    /// <summary>Half the width of the flat plates, and the radius of the round ones.</summary>
    private const float Half = 1.5f;

    /// <summary>How thick the two closed controls are. Small enough that their silhouette is the plate's.</summary>
    private const float Thickness = 0.06f;

    private readonly string _shape = Env("AVA3D_PROBE_SHAPE", "quad");
    private readonly float _height = Num("AVA3D_PROBE_H", 3f);
    private readonly bool _showLines = Env("AVA3D_PROBE_LINES", "1") != "0";

    private Node _casterPivot = null!;
    private Node _bulb = null!;
    private LineNode _rays = null!, _outline = null!, _crosses = null!, _edges = null!, _drop = null!;
    private PointLight _lamp = null!;

    private Vector3 _light;
    private float _spin, _tilt;

    public override string Title => "Shadow probe";

    public override string Summary => "The shadow, and the outline it is supposed to have";

    public override string Notes =>
        """
        One flat caster, one flat floor, one point light, and the answer drawn on top of the picture.

        The yellow lines are rays from the bulb through the caster's silhouette, carried on until they meet
        the floor. The red crosses are where they land. The cyan loop through the crosses is the shadow's
        outline as geometry demands it: for a point light at L, a silhouette vertex P lands at
        L + (P − L) · L.y / (L.y − P.y), which for a four-cornered plate is four points and a quadrilateral.
        The grey shadow underneath is what the depth map and the shader actually produced. The two should
        coincide, and where they do not the gap is readable off the floor in world units.

        Both sides are closed form. That matters more than it sounds: the previous three attempts at
        verifying this feature compared renderers with each other or eyeballed a busy scene, and all three
        signed off a shadow map that was being read upside down.

        Four shapes, in two pairs — quad and circle are flat plates with no thickness, slab and puck are
        solids six centimetres thick with the same silhouette. Their predicted outlines are the same to
        within that thickness, so any difference between a pair comes from the depth pass and not from the
        arithmetic. There is one, and it is the documented cost of second-depth shadowing: only faces turned
        away from the light are written to the map, and a single plane turned towards it has none. The quad's
        shadow is therefore missing for half of every rotation, while the slab's is continuous. That is a
        limitation of the technique rather than a mistake in it, and this is the scene that shows it.

        Set AVA3D_PROBE_SHAPE, AVA3D_PROBE_ANGLE, AVA3D_PROBE_TILT and AVA3D_PROBE_LIGHT to freeze any one
        configuration; tools/shadow-probe.sh sweeps all of them and reports the error as a number.
        """;

    /// <summary>
    /// What the caster is doing and whether it can cast at all, because "no shadow" is the correct answer
    /// here often enough that a viewer needs telling which time this is.
    /// </summary>
    public override string? Caption
    {
        get
        {
            var solid = _shape is "slab" or "puck";
            var facing = FacesTheLight();

            return $"{_shape} · spin {_spin,3:F0}° · tilt {_tilt,4:F0}° — " + (solid
                ? "a closed solid, so it always has a face turned away from the light: casting"
                : facing
                    ? "flat, and turned towards the light. It has no back face for the depth pass to keep, "
                      + "so it casts nothing"
                    : "flat, with its back to the light: casting");
        }
    }

    /// <summary>
    /// Whether the plate's lit side is the one the bulb can see. The depth pass keeps only faces turned
    /// away from the light, so for a caster with no thickness this is the difference between a shadow and
    /// none at all.
    /// </summary>
    private bool FacesTheLight()
    {
        var normal = Vector3.Transform(Vector3.UnitY, _casterPivot.Rotation);
        var toLight = _light - _casterPivot.Position;

        return Vector3.Dot(normal, toLight) > 0f;
    }

    /// <summary>Frozen the moment anything is pinned by hand, so a sweep photographs what it asked for.</summary>
    public override bool Animates => !Frozen;

    public override bool FramesItself => true;

    private static bool Frozen =>
        Has("AVA3D_PROBE_ANGLE") || Has("AVA3D_PROBE_TILT") || Has("AVA3D_PROBE_LIGHT");

    public override void Frame(Camera camera)
    {
        // Nearly a plan view, and that is not for looks. The check that matters is done by un-projecting
        // every shadowed pixel back onto the floor, and a camera looking almost straight down leaves almost
        // no perspective to undo and no part of the shadow hidden behind the caster that threw it.
        camera.Target = new Vector3(-1.5f, 0f, -1f);
        camera.Distance = 26f;
        camera.Yaw = 0f;
        camera.Pitch = 1.20f;
        camera.NearPlane = 1f;
        camera.FarPlane = 90f;
        camera.FieldOfView = 32f;

        // The harness pins the camera exactly, because un-projection needs the numbers it was taken with.
        if (Environment.GetEnvironmentVariable("AVA3D_PROBE_CAM") is { Length: > 0 } pinned)
        {
            var p = pinned.Split(',');
            if (p.Length >= 4)
            {
                camera.Yaw = Parse(p[0]);
                camera.Pitch = Parse(p[1]);
                camera.Distance = Parse(p[2]);
                camera.FieldOfView = Parse(p[3]);
                camera.Target = p.Length >= 7
                    ? new Vector3(Parse(p[4]), Parse(p[5]), Parse(p[6]))
                    : Vector3.Zero;
            }
        }
    }

    public override void Stage(Scene scene)
    {
        scene.Background = Color.FromRgb(10, 11, 15);

        // The key light is kept but turned off, because the ambient floor is read from the first
        // directional light in the list and removing it would take the floor with it. Nothing else here
        // should light anything: the point light has to be the only thing casting light as well as the only
        // thing casting shadow, or the shadow is a grey patch inside a lit one and its edge is a guess.
        scene.Light.Intensity = 0f;
        scene.Light.Ambient = 0.05f;
        scene.Light.CastsShadows = false;

        var start = LightPosition(0d);

        _lamp = new PointLight
        {
            Position = start,
            Color = Vector3.One,
            Intensity = IntensityFor(start),
            Decay = 2f,
            CastsShadows = true
        };

        scene.Lights.Add(_lamp);

        scene.Environment = EnvironmentLight.None;
        scene.ShadowMapSize = (int)Num("AVA3D_PROBE_MAP", 2048f);
        scene.ShadowStrength = 1f;

        if (Has("AVA3D_PROBE_BIAS"))
            scene.ShadowBias = Num("AVA3D_PROBE_BIAS", 0.0015f);
    }

    public override Node? BuildSubject()
    {
        var root = new Node { Name = "probe" };

        // Segmented, because the CPU renderer shades per vertex and a floor made of two triangles has no
        // interior to put a shadow on. Sixty-four is plenty for the GPU backends and cheap enough to leave
        // on for them; a software cross-check raises it.
        var segments = (int)Num("AVA3D_PROBE_SEG", 64f);

        // Fourteen units, not the forty a room would have. The light frustum is fitted to the whole
        // scene's bounding sphere, so a floor larger than the shadows need is a coarser shadow map for no
        // gain — which is a real defect of the fit and not a reason to hide it here, but this scene is the
        // ruler and a ruler should not have the fault it is measuring built into it.
        var floor = Num("AVA3D_PROBE_FLOOR", 14f);

        root.Children.Add(new MeshNode(
            Primitives.Plane(floor, floor, segments, segments),
            new Material { BaseColor = new Vector4(0.78f, 0.78f, 0.80f, 1f), Roughness = 1f })
        {
            Name = "floor",
            Position = new Vector3(0f, FloorY, 0f),
            CastsShadow = false
        });

        _casterPivot = new Node { Name = "caster", Position = new Vector3(0f, _height, 0f) };

        _casterPivot.Children.Add(new MeshNode(CasterMesh(), new Material
        {
            BaseColor = new Vector4(0.92f, 0.42f, 0.30f, 1f),
            Roughness = 0.75f
        }));

        root.Children.Add(_casterPivot);

        // The bulb, drawn as an object so the rays visibly start somewhere. Unlit, or the thing that is the
        // source of all the light in the scene would have a dark side.
        _bulb = new MeshNode(Primitives.Sphere(0.16f, 20, 14), new Material
        {
            BaseColor = new Vector4(1f, 0.93f, 0.62f, 1f),
            Unlit = true
        })
        {
            Name = "bulb",
            CastsShadow = false,

            // Part of the overlay, not part of the scene, and hidden with the rest of it. The light
            // frustum is fitted to the scene's bounding sphere, and a mesh sitting at the light is the
            // furthest thing in the scene from everything else — leaving it visible for a measured run
            // moved the sphere so far that the frustum's near plane collapsed and the shadow changed. An
            // instrument that alters what it is measuring is worse than no instrument.
            IsVisible = _showLines
        };

        root.Children.Add(_bulb);

        _rays = Overlay(root, new Vector3(1.00f, 0.80f, 0.12f), 2f, 0.85f);
        _edges = Overlay(root, new Vector3(0.35f, 1.00f, 0.50f), 2f, 0.9f);
        _crosses = Overlay(root, new Vector3(1.00f, 0.22f, 0.18f), 3f, 1f);
        _outline = Overlay(root, new Vector3(0.15f, 0.85f, 1.00f), 3f, 1f);
        _drop = Overlay(root, new Vector3(0.55f, 0.58f, 0.65f), 1f, 0.5f);

        Place(0d);

        return root;
    }

    public override void Update(Scene scene, double elapsed)
    {
        Place(elapsed);
        scene.Invalidate();
    }

    /// <summary>
    /// Puts the light and the caster where this moment says, then redraws every debug line from where they
    /// ended up. One method rather than two so the lines can never describe last frame's geometry.
    /// </summary>
    private void Place(double elapsed)
    {
        _light = LightPosition(elapsed);
        _spin = Has("AVA3D_PROBE_ANGLE") ? Num("AVA3D_PROBE_ANGLE", 0f) : (float)(elapsed * 24d);
        _tilt = Has("AVA3D_PROBE_TILT")
            ? Num("AVA3D_PROBE_TILT", 0f)
            : 28f * MathF.Sin((float)elapsed * 0.42f);

        if (_lamp is not null)
        {
            _lamp.Position = _light;

            // Brightness tracks the height, because the falloff is inverse-square and this scene exists to
            // be compared across light positions. Left at one number, a lamp moved twice as far up lights
            // the floor a quarter as well, and two captures that differ only in how dark they are cannot be
            // told apart from two that differ in where the shadow is.
            _lamp.Intensity = IntensityFor(_light);
        }

        _bulb.Position = _light;

        const float toRadians = MathF.PI / 180f;

        // Yaw about the world's up and then tilt about the world's X, in that order, so the printed pair of
        // angles means the same thing whichever value the other one holds. Composed the other way round the
        // tilt would swing with the spin and a sweep's table would be unreadable.
        _casterPivot.Rotation =
            Quaternion.CreateFromAxisAngle(Vector3.UnitX, _tilt * toRadians) *
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, _spin * toRadians);

        if (!_showLines)
            return;

        var world = Matrix4x4.CreateFromQuaternion(_casterPivot.Rotation) *
                    Matrix4x4.CreateTranslation(_casterPivot.Position);

        var silhouette = Silhouette();
        var corners = new Vector3[silhouette.Length];

        for (var i = 0; i < silhouette.Length; i++)
            corners[i] = Vector3.Transform(silhouette[i], world);

        // The caster's own outline, in green, so the shape being projected is as visible as its projection.
        var edges = new List<Vector3>(corners.Length * 3);

        if (_shape is "cube")
        {
            // Bottom ring, top ring, and the four uprights between them.
            for (var i = 0; i < 4; i++)
            {
                edges.Add(corners[i]);
                edges.Add(corners[(i + 1) % 4]);
                edges.Add(corners[4 + i]);
                edges.Add(corners[4 + (i + 1) % 4]);
                edges.Add(corners[i]);
                edges.Add(corners[4 + i]);
            }
        }
        else
        {
            for (var i = 0; i < corners.Length; i++)
            {
                edges.Add(corners[i]);
                edges.Add(corners[(i + 1) % corners.Length]);
            }
        }

        _edges.Positions = edges.ToArray();

        var landed = new List<Vector3>(corners.Length);
        var rays = new List<Vector3>(corners.Length * 2);
        var crosses = new List<Vector3>(corners.Length * 4);

        // One ray per silhouette vertex for a quad — the four the shadow is made of. A circle would be
        // sixty-four lines and unreadable, so it draws every fifth and keeps all sixty-four for the loop.
        var everyNth = Math.Max(1, corners.Length / 12);

        for (var i = 0; i < corners.Length; i++)
        {
            if (Cast(_light, corners[i]) is not { } hit)
                continue;

            landed.Add(hit);

            if (i % everyNth != 0)
                continue;

            rays.Add(_light);
            rays.Add(hit);

            // A cross rather than a dot: a dot at this size is one or two pixels and disappears against a
            // shadow edge, which is the one place it has to be legible.
            crosses.Add(hit + new Vector3(-0.18f, 0.004f, 0f));
            crosses.Add(hit + new Vector3(0.18f, 0.004f, 0f));
            crosses.Add(hit + new Vector3(0f, 0.004f, -0.18f));
            crosses.Add(hit + new Vector3(0f, 0.004f, 0.18f));
        }

        _rays.Positions = rays.ToArray();
        _crosses.Positions = crosses.ToArray();

        // The hull of where the rays landed, rather than the order the vertices were listed in. For a flat
        // plate the two are the same loop; for a solid they are not, and the hull is the one that is right —
        // the shadow of a convex body is the convex hull of its shadow.
        var ring = Hull(landed);

        // Lifted four millimetres off the floor. Drawn exactly on it, a line and the surface it lies on
        // round to the same depth and the loop dashes in and out along its length.
        var loop = new List<Vector3>(ring.Count * 2);

        for (var i = 0; i < ring.Count; i++)
        {
            loop.Add(ring[i] with { Y = FloorY + 0.004f });
            loop.Add(ring[(i + 1) % ring.Count] with { Y = FloorY + 0.004f });
        }

        _outline.Positions = loop.ToArray();

        _drop.Positions =
        [
            _light,
            new Vector3(_light.X, FloorY + 0.004f, _light.Z)
        ];
    }

    /// <summary>
    /// The convex hull of points on the floor, wound counter-clockwise. Monotone chain: sort, then sweep
    /// the lower side and the upper side, dropping any point the two before it already turn past.
    /// </summary>
    private static List<Vector3> Hull(List<Vector3> points)
    {
        if (points.Count < 3)
            return points;

        var sorted = new List<Vector3>(points);
        sorted.Sort((a, b) => a.X != b.X ? a.X.CompareTo(b.X) : a.Z.CompareTo(b.Z));

        var hull = new List<Vector3>(sorted.Count + 1);

        for (var pass = 0; pass < 2; pass++)
        {
            var start = hull.Count;

            foreach (var p in pass == 0 ? sorted : Enumerable.Reverse(sorted))
            {
                while (hull.Count - start >= 2 && Turn(hull[^2], hull[^1], p) <= 0)
                    hull.RemoveAt(hull.Count - 1);

                hull.Add(p);
            }

            hull.RemoveAt(hull.Count - 1);
        }

        return hull;

        static float Turn(Vector3 a, Vector3 b, Vector3 c) =>
            (b.X - a.X) * (c.Z - a.Z) - (b.Z - a.Z) * (c.X - a.X);
    }

    /// <summary>
    /// Where a point light at <paramref name="light"/> throws <paramref name="p"/> onto the floor, or null
    /// when there is no such place because the light is level with the vertex or below it.
    ///
    /// This is the whole prediction. The ray is <c>L + t(P − L)</c>; its height is zero when
    /// <c>t = L.y / (L.y − P.y)</c>, and that t is greater than one for any vertex between the light and
    /// the floor, which is to say the shadow is always further from the light than the thing throwing it.
    /// </summary>
    private static Vector3? Cast(Vector3 light, Vector3 p)
    {
        var drop = light.Y - p.Y;

        if (drop <= 1e-4f)
            return null;

        return light + (p - light) * ((light.Y - FloorY) / drop);
    }

    /// <summary>
    /// The caster's outline in its own space, wound once around. For the flat pair this is exactly the
    /// geometry; for the two solids it is the silhouette they share with it, which is why a pair can be
    /// compared against one prediction.
    /// </summary>
    private Vector3[] Silhouette()
    {
        // A box has no fixed silhouette: which of the eight corners are on the outline depends on where the
        // light is. All eight are handed over and the hull of where they land sorts it out, which is exact
        // for any convex solid and is why this returns a set rather than a loop.
        if (_shape is "cube")
            return
            [
                new Vector3(-Half, -Half, -Half), new Vector3(Half, -Half, -Half),
                new Vector3(Half, -Half, Half), new Vector3(-Half, -Half, Half),
                new Vector3(-Half, Half, -Half), new Vector3(Half, Half, -Half),
                new Vector3(Half, Half, Half), new Vector3(-Half, Half, Half)
            ];

        if (_shape is "quad" or "slab")
            return
            [
                new Vector3(-Half, 0f, -Half),
                new Vector3(Half, 0f, -Half),
                new Vector3(Half, 0f, Half),
                new Vector3(-Half, 0f, Half)
            ];

        var points = new Vector3[64];

        for (var i = 0; i < points.Length; i++)
        {
            var theta = i / (float)points.Length * MathF.Tau;
            points[i] = new Vector3(MathF.Cos(theta) * Half, 0f, MathF.Sin(theta) * Half);
        }

        return points;
    }

    private Mesh CasterMesh() => _shape switch
    {
        "cube" => Primitives.Box(Half * 2f, Half * 2f, Half * 2f),
        "circle" => Primitives.Disc(Half, 64),
        "slab" => Primitives.Box(Half * 2f, Thickness, Half * 2f),
        "puck" => Primitives.Cylinder(Half, Half, Thickness, 64),
        _ => Primitives.Plane(Half * 2f, Half * 2f)
    };

    /// <summary>
    /// What the lamp has to be worth at this height for the floor under it to come out mid-grey. The
    /// falloff is <c>1/d²</c>, so this is <c>k·y²</c> and k was read off a picture.
    /// </summary>
    private static float IntensityFor(Vector3 light) => 1.4f * light.Y * light.Y;

    private static Vector3 LightPosition(double elapsed)
    {
        if (Environment.GetEnvironmentVariable("AVA3D_PROBE_LIGHT") is { Length: > 0 } pinned)
        {
            var p = pinned.Split(',');

            if (p.Length >= 3)
                return new Vector3(Parse(p[0]), Parse(p[1]), Parse(p[2]));
        }

        // Round the caster once every twenty-six seconds, rising and falling as it goes, so a shadow is
        // seen at every angle and every steepness without anybody touching a control.
        var a = (float)elapsed * 0.24f;

        return new Vector3(MathF.Cos(a) * 5.5f, 7.6f + MathF.Sin(a * 0.7f) * 1.8f, MathF.Sin(a) * 5.5f);
    }

    /// <summary>
    /// A line layer over the scene. DepthWrite off and RenderOrder 1 is the recipe for lines that belong to
    /// a surface: the first stops them fighting the floor in the depth buffer, the second draws them after
    /// it however the two origins compare.
    /// </summary>
    private static LineNode Overlay(Node root, Vector3 color, float width, float opacity)
    {
        var node = new LineNode
        {
            Positions = [],
            Color = color,
            Width = width,
            Opacity = opacity,
            DepthWrite = false,
            RenderOrder = 1
        };

        root.Children.Add(node);

        return node;
    }

    private static bool Has(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 };

    private static string Env(string name, string fallback) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : fallback;

    private static float Num(string name, float fallback) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value &&
        float.TryParse(value, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;

    private static float Parse(string text) =>
        float.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
}
