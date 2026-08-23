# QuestMarkers

World-anchored HUD markers for your unfinished quest objectives: zones to
visit, spots to place items or beacons at, and quest items lying in the raid.
No more running circles around a vague quest description - the marker floats
where the objective is, with the quest name and the distance.

## Main features

- **Markers anchored in the world.** Objectives on screen get a pin whose tip
  sits on the spot, with the quest name and distance below it; objectives off
  screen get an arrow at the screen border pointing toward them. Distant
  markers are drawn slightly smaller, which reads as depth rather than as a
  flat overlay.
- **An icon per kind of objective**, in its own colour, on the pin and on the
  edge arrow: a gold flag for a place to reach, a green crate for something
  to leave behind, a violet antenna for a beacon, an orange burst for a
  flare, a blue case for a quest item to find.
- **A glance, not a permanent overlay.** The markers show for a few seconds
  per keypress and then fade out again (configurable, including "stay").
- **Only what is still open.** Completed conditions disappear, picked-up
  quest items too. Objectives of other maps never show.
- **Click-through glass.** The HUD ignores mouse and keyboard entirely; the
  game stays fully playable while it is visible.
- **Honest when the picture lies.** While aiming through a magnified optic
  (whose lens renders its own camera) the markers fade out instead of
  pointing at wrong pixels. Scav raids show no PMC quest markers.

## Requirements and compatibility

- SPT 4.1 (client mod only, nothing to install server-side).
- [Anvil-WebOverlay](https://sp-mod.com/mod/2879/weboverlay) library 1.8.2 or
  newer (hard dependency)
  and the Microsoft WebView2 runtime it needs - current Windows 10/11 already
  includes it.
- Borderless windowed or windowed mode (exclusive fullscreen cannot show an
  overlay over the game).
- Fika: not tested. The mod only reads the local player's quest data and
  draws locally, so it is expected to work, but that is unverified.

## Installation

Extract the release zip over your SPT folder; it places
`BepInEx/plugins/maschine-QuestMarkers.dll`. Install the Anvil-WebOverlay zip
first if you do not have it yet.

## Usage and default controls

**F7** shows the markers for six seconds - a quick glance to orient
yourself, then they are out of the way again. The key works while moving,
and the same peek happens automatically when a raid starts. Set *Auto-hide
after* to 0 if you would rather have them stay until you toggle them off.

| Section | Option | Default | Meaning |
|---|---|---|---|
| General | Show in raid automatically | on | Show the markers whenever a raid starts. |
| General | Toggle key | F7 | Show or hide the markers during a raid. |
| Filter | Maximum distance | 0 (unlimited) | Hide markers farther away than this many meters. |
| Filter | Maximum markers | 16 | At most this many markers at once; the closest objectives win. |
| Display | Show quest names | on | Print the quest name under each marker. |
| Display | Show distances | on | Print the distance under each marker. |
| Display | Edge arrows | on | Point at off-screen objectives with border arrows. |
| Display | Auto-hide after | 6 s | Hide the markers again this many seconds after they appeared. 0 keeps them visible. |
| Display | Marker size | 1.8 | Overall size of the markers. |
| Display | Size at distance | 0.5 | How large a far marker is compared to a close one; 1 makes every marker the same size. |
| Display | Distance for smallest size | 250 m | Where that smallest size is reached. |

## Known limitations

- Markers show through walls - there is no occlusion. For "go there"
  guidance that is usually what you want.
- Kill counters, handovers, skill and trader conditions have no world
  position and therefore no marker. "Find item" means quest items lying in
  the raid, not found-in-raid collection quests; a condition that asks for an
  item category rather than a specific item gets no marker either.
- A zone id that exists several times in a map is shown as one marker at the
  combined center.
- Quest items inside containers are only known to the game once the
  container has been opened, so their marker can appear late.

## Support

Please include your exact SPT and mod versions, what you expected, what
happened instead, and the client log
(`BepInEx/LogOutput.log`) when reporting a problem.

## License, credits, third-party

MIT. Renders through the [Anvil-WebOverlay](https://github.com/maschine34675/WebOverlay)
library. The quest-condition walk follows the approach proven by the
DynamicMaps and GTFO mods (both MIT) - thanks to their authors; no code was
copied.
