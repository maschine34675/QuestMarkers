# QuestMarkers

World-anchored markers for your unfinished quest objectives. Press **F7** and
for a few seconds every open objective on this map shows where it is - a zone to
visit, a spot to plant an item or beacon, a quest item lying somewhere - with
the quest name and the distance. Off-screen objectives get an arrow at the
screen border, and the markers fade out again so they never clutter the raid.

## Features

- **Pins in the world**, not on a map: their tip sits on the objective, with
  quest name and distance underneath; distant ones draw slightly smaller.
- **One icon and colour per kind** of objective - gold flag for a place to
  reach, green crate for an item to leave, violet antenna for a beacon, orange
  burst for a flare, blue case for a quest item to find - on the pins and on
  the edge arrows.
- **A glance, not an overlay.** Six seconds per keypress (configurable, or
  keep them on), and the same peek automatically at raid start.
- **Only what is still open**: completed conditions and picked-up quest items
  disappear; objectives of other maps never show.
- **Stays out of the way**: click-through glass that hides while you are
  dead, in a menu, in third person or looking through a magnified optic.

## Installation and first use

Install [Anvil-WebOverlay](https://sp-mod.com/mod/2879/weboverlay) first,
then extract this zip over your SPT folder (`BepInEx/plugins/maschine-QuestMarkers.dll`).
Start a raid: the markers appear for six seconds, and F7 brings them back.
Everything else - distance and marker limits, labels, arrows, auto-hide, size
- is in the F12 configuration menu under *maschine-QuestMarkers*.

## Requirements and compatibility

- SPT 4.1, client mod only - nothing to install on the server.
- Anvil-WebOverlay 1.8.2 or newer and the Microsoft WebView2 runtime (current
  Windows 10/11 already includes it).
- Borderless windowed or windowed mode; exclusive fullscreen cannot show an
  overlay over the game.
- Fika: not tested. The mod only reads the local player's quest data and draws
  locally, so it is expected to work, but that is unverified.

## Limitations

- Markers show through walls - there is no occlusion.
- Kill, handover, skill and trader conditions have no position and get no
  marker; "find item" means quest items lying in the raid, not found-in-raid
  collection quests.
- A zone id that exists several times on a map is shown once, at the combined
  center.

## Support

Report problems with your exact SPT and mod versions, what you expected and
what happened, and the client log `BepInEx/LogOutput.log`.
