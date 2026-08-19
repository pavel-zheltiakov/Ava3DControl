using System.Numerics;

namespace Ava3D.Demo.Story;

/// <summary>
/// Chapter 0. Black, one cube, one lamp, and a circle of floor.
///
/// Nothing is hidden here — there is simply nothing yet to hide it from. The room is standing, the walls
/// are two metres behind him and the other three lamps are over his head, and none of it is lit, so none
/// of it is there. That is the demonstration as much as the cube is: a room with its lights off is the
/// only honest picture of what a light contributes, and it is the picture chapter 1 takes away.
/// </summary>
internal sealed class Dark(Antechamber room) : Chapter
{
    public override string Title => "Dark";

    /// <summary>
    /// How long it runs. <b>Sixteen, and it was twenty-five.</b>
    ///
    /// Nine seconds came out of the front of the film and every one of them was a held frame. The chapter's
    /// whole job is to be a room with its lights off — it has one cube, one lamp and a camera that takes two
    /// steps back — and a shot that has made its point goes on making it for as long as it is up. Twenty-five
    /// seconds of that is the first thing anybody sees of this control, and what it says about the control is
    /// that it is slow. It says it before a single feature is on screen.
    ///
    /// Nothing was cut. All three captions are here, the step back is here, the cube still turns through the
    /// same faces. What changed is the dwell either side of each of them, which is the part of a shot nobody
    /// can name and everybody feels.
    /// </summary>
    public override float Duration => 16f;

    /// <summary>
    /// Still for five seconds, then two steps back — far enough to learn the darkness has a floor, not
    /// far enough to find a wall.
    /// </summary>
    public override Walk Walk { get; } = new(
        new Step(0f, new Vector3(0f, Deck.Eye, -2.1f), new Vector3(0f, 1.28f, 0f)),
        new Step(5f, new Vector3(0f, Deck.Eye, -2.1f), new Vector3(0f, 1.28f, 0f)),
        new Step(9f, new Vector3(0f, Deck.Eye, -2.8f), new Vector3(0f, 1.20f, 0f)),
        new Step(16f, new Vector3(0f, Deck.Eye, -2.8f), new Vector3(0f, 1.20f, 0f)));

    public override void Enter(Hall hall)
    {
        hall.Occupy(Deck.AntechamberRoom);

        // One slot spent, three left unspent. A dark room is not a room lit by four dim lamps.
        hall.Use(room.Spot.Light);
        hall.Ambient(0.008f, 0.004f);

        room.Spot.Dim(1f);

        foreach (var lamp in room.House)
            lamp.Dim(0f);
    }

    public override void Update(Hall hall, float seconds)
    {
        // One turn over the chapter, so the cube is a solid rather than a silhouette — three faces are
        // visible at some point and the light moves across all of them.
        room.Cube.RotationDegrees = new Vector3(0f, seconds * 14f, 0f);
        hall.Scene.Invalidate();
    }

    /// <summary>
    /// The captions are the watch, not the renderer.
    ///
    /// They used to name what was on screen — one box, one light, default everything — and that was a
    /// column of specifications running under a film. Everything technical this building has to say is
    /// said by <see cref="DemoScene.Notes"/> on the feature it belongs to, where somebody who wants it
    /// will go looking; down here there is a person on a night watch on a ship, and what he says is what
    /// he would say.
    ///
    /// <b>All ten chapters are one document, and it is a handover note.</b> He is on his last night after
    /// nine hundred of them, he has been asked for four hundred words for whoever replaces him, and the
    /// film is him failing to write it. That frame is invisible from inside any one file, so the four
    /// things it constrains are listed here, where the first caption is:
    ///
    /// <list type="number">
    /// <item><b>Rules one to six, one per room, and then never again.</b> Chapters 1 to 6 each open on a
    /// numbered rule; chapter 7 has none, because the window ends the note. A rule added or dropped
    /// anywhere renumbers the rest, and <see cref="Repair"/>'s is the last one.</item>
    /// <item><b>Three things run late on one night, and one machine explains all three</b> — the turning
    /// display in <see cref="Forms"/>, the mirror ball in <see cref="Screens"/>, and the dead machine in
    /// <see cref="Repair"/>, which says so out loud. The word is <i>late</i> in all three, deliberately:
    /// it is what makes the connection findable by somebody reading in a second language. Nothing here
    /// is connected to <c>Contact</c>, and no caption may hint that it is.</item>
    /// <item><b>The lamp over the first crate is the ending.</b> It is on in this chapter, explained in
    /// <see cref="Houselights"/>, and handed on in <see cref="Morning"/>'s last line — which is also the
    /// caption that holds through the free walk, so it has to read as an instruction.</item>
    /// <item><b>Rule 6 of the plan still stands.</b> The word <i>ship</i> first appears in
    /// <see cref="Outside"/>, at the window, and nowhere before it.</item>
    /// </list>
    ///
    /// The cue times are the walk's, not the prose's: a caption fires when the thing it names is in
    /// frame, so moving a waypoint moves a caption with it.
    /// </summary>
    public override string? Caption(float seconds) => seconds switch
    {
        < 2f => null,
        < 6.5f => "Notes for the next person. They asked for four hundred words",
        < 11f => "This is my night number nine hundred. And my last one",
        // "4AM" rather than "Four in the morning". The caption band draws its own face and has digits in
        // it, and a numeral is read at a glance where four spelled-out words have to be read — which is
        // what a caption on screen for eight seconds gets. It also takes the line from seventy characters
        // to fifty-four, which is the difference between two lines and one on a phone.
        _ => "4AM. Someone left a lamp on over the first crate again"
    };
}
