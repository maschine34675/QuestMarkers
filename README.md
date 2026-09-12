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
  flare, a blue case for a quest item to find, a rose door for an extract a
  quest wants you to leave through.
- **A glance, not a permanent overlay.** The markers show for a few seconds
  per keypress and then fade out again (configurable, including "stay").
- **Only what is still open.** Completed conditions disappear, picked-up
  quest items too. Objectives of other maps never show.
- **Click-through glass.** The HUD ignores mouse and keyboard entirely; the
  game stays fully playable while it is visible.
- **Your extracts too.** The extracts this raid gives you - the same list
  the game shows on the exfil panel - appear as diamonds, so they never pass
  for a quest objective: green when open, amber and pulsing while a
  countdown runs, grey while they are not confirmed open - a switch, a car
  or a train still outstanding, or an exit left to chance, whose roll the
  markers never give away. An extract a quest sends you to keeps its rose
  quest pin and shows the same state on it. Secret exits appear only once
  you have found them. Playing a scav you get the exits that raid claimed
  for you, the same ones its panel lists. Turn *Show quest markers* off and
  the extracts are all that is left.
- **Honest when the picture lies.** While aiming through a magnified optic
  (whose lens renders its own camera) the markers fade out instead of
  pointing at wrong pixels. A scav gets extract markers but no quest
  markers, because a scav carries no PMC quest log.

## Requirements and compatibility

- SPT 4.1 (client mod only, nothing to install server-side).
- [Anvil-WebOverlay](https://sp-mod.com/mod/2879/weboverlay) library 1.11.0 or
  newer (hard dependency) and the Microsoft WebView2 runtime it needs -
  current Windows 10/11 already includes it.
- Borderless windowed or windowed mode (exclusive fullscreen cannot show an
  overlay over the game).
- Fika: not tested. The mod only reads the local player's quest data and
  draws locally, so it is expected to work, but that is unverified.

## Installation

Extract the release zip over your SPT folder; it places the single file
`BepInEx/plugins/maschine-QuestMarkers.dll`. Install the Anvil-WebOverlay zip
first if you do not have it yet.

## Usage and default controls

**I** shows the markers for six seconds - a quick glance to orient
yourself, then they are out of the way again. The key works while moving,
and the same peek happens automatically when a raid starts. It is also the
Quest Tracker mod's panel key, so with both installed one press gives you
the list and the markers at once. Set *Auto-hide after* to 0 if you would
rather have them stay until you toggle them off.

The options below are named as the F12 settings menu shows them; the keys in
`BepInEx/config/com.maschine.QuestMarkers.cfg` keep their own spelling.

| Section | Option | Default | Meaning |
|---|---|---|---|
| General | Toggle Key | I | Show or hide the markers during a raid. |
| General | Show At Raid Start | on | Show the markers whenever a raid starts. |
| Filter | Show Quest Markers | on | Mark the objectives of your started quests. Off keeps only the extract markers. |
| Filter | Show My Extracts | on | Also mark the extracts this raid gives you - as a scav, the ones claimed for you - with their open, locked or countdown state. |
| Filter | Maximum Quest Markers | 16 | At most this many quest markers at once; the closest objectives win. Extracts do not count. |
| Filter | Maximum Distance (m) | 0 (unlimited) | Hide markers farther away than this many meters, extracts included. |
| Display | Show Quest Names | on | Print the quest name under each marker. |
| Display | Show Distances | on | Print the distance under each marker. |
| Display | Arrows For Off-Screen Targets | on | Point at off-screen objectives with border arrows. |
| Display | Auto-Hide After (s) | 6 s | Hide the markers again this many seconds after they appeared. 0 keeps them visible. |
| Display | Marker Size | 1.8 | Overall size of the markers. |
| Display | Size At Distance | 0.5 | How large a far marker is compared to a close one; 1 makes every marker the same size. |
| Display | Distance For Smallest Size (m) | 250 m | Where that smallest size is reached. |
| Display | Use The Bundled Font | on | Label the markers in Chakra Petch, a squarish technical face that suits the game better than the Windows one. |

## Known limitations

- Markers show through walls - there is no occlusion. For "go there"
  guidance that is usually what you want.
- Kill counters, handovers, skill and trader conditions have no world
  position and therefore no marker, and a "survive and extract" condition
  that names no particular exit gets none either - any extract will do.
  "Find item" means quest items lying in the raid, not found-in-raid
  collection quests; a condition that asks for an item category rather than
  a specific item gets no marker either.
- A zone id that exists several times in a map is shown as one marker at the
  combined center.
- Extract markers show whether an exit is open, not what it asks of you: a
  paracord or payment requirement stays invisible until you step in, as in
  vanilla. An exit left to chance stays grey for the whole raid - the exfil
  panel tells you once you have stood in it - and the co-op exit stays grey
  until a scav and a PMC start its countdown together. While you are committed
  to a transit, the extract markers are gone, as the game closes every exit
  to you then.
- Quest items inside containers are only known to the game once the
  container has been opened, so their marker can appear late.
- Driver-level frame generation (AMD Fluid Motion Frames, Lossless Scaling,
  NVIDIA Smooth Motion) can stutter while the markers are up: any window
  over the game changes how Windows presents the game's frames, and the
  frame generator's pacing suffers, at an unchanged frame rate. Reported on
  AMD Fluid Motion Frames 2.1 and gone with that setting off; upscaling
  without frame generation is unaffected.

## Support

Please include your exact SPT and mod versions, what you expected, what
happened instead, and the client log
(`BepInEx/LogOutput.log`) when reporting a problem.

## License, credits, third-party

MIT. Renders through the [Anvil-WebOverlay](https://github.com/maschine34675/WebOverlay)
library. The quest-condition walk follows the approach proven by the
DynamicMaps and GTFO mods (both MIT) - thanks to their authors; no code was
copied.

The markers are labelled in **Chakra Petch** by Cadson Demak, redistributed
unchanged under the SIL Open Font License 1.1. Rather than leaving a text
file in your plugins folder, both the font and its full licence live inside
the plugin: the notice is in the assembly's copyright field, which Windows
shows under *Properties - Details - Copyright*, the licence text is an
embedded resource named `QuestMarkers.font-license.txt`, and the plugin
names the font in the log at startup. `web/FONT-SOURCES.md` records where the
file came from, its upstream commit and its checksum.
