using System.Numerics;

namespace Ava3D.Demo.Scenes.Contact;

/// <summary>One shot of the film: when it runs, what it says, and where it puts the camera.</summary>
/// <param name="Start">Seconds into the cycle at which the shot cuts in.</param>
/// <param name="End">Seconds at which the next one cuts in.</param>
/// <param name="Caption">A line for the bottom of the frame, shown for the first few seconds. Null for none.</param>
/// <param name="Aim">Given time through the shot, 0..1, aims the camera.</param>
internal readonly record struct Shot(float Start, float End, string? Caption, Action<Camera, float> Aim);

/// <summary>
/// The camera rig.
///
/// <see cref="Camera"/> is an orbit camera — a target, a distance and two angles — which is the right
/// default for a viewer control and not the way a film camera is described. A film camera is an eye and a
/// thing it is looking at, and <see cref="Camera.LookFrom"/> is exactly that, so every shot below is
/// written in those terms and nothing here has to convert.
///
/// This file used to hold that conversion, and a note saying the film had no camera roll because an orbit
/// camera has none to give. Both are gone: <see cref="Camera.Roll"/> exists now, and the two shots that
/// ride a ship hand <see cref="Camera.RollToward"/> that ship's own up axis, so the horizon banks with the
/// wings instead of staying level under a rolling aircraft.
///
/// The two easing curves that used to live here are gone the same way. Every shot below eases, so they
/// were never about this film — <see cref="Ease.InOut"/> is the same smoothstep under a name that says
/// which end is soft.
/// </summary>
internal static class Shots
{
    /// <summary>A point on a circle around <paramref name="centre"/>, at a height above it.</summary>
    public static Vector3 Around(Vector3 centre, float radius, float angle, float height) =>
        centre + new Vector3(MathF.Cos(angle) * radius, height, MathF.Sin(angle) * radius);

    /// <summary>
    /// A point in a ship's own frame rather than the world's, for a camera that rides along with it.
    ///
    /// Behind and above in the ship's frame stays behind and above through a banked turn, which is what
    /// makes a chase shot read as a chase rather than as a camera the ship happens to be flying past.
    /// </summary>
    public static Vector3 Rider(in Flight.Pose pose, Vector3 offset) =>
        pose.Position + Vector3.Transform(offset, pose.Rotation);
}
