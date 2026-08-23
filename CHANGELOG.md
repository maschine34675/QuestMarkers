# Changelog

## [Unreleased]

## [1.0.0]

### Forge version notes

- First release: world-anchored markers for your unfinished quest objectives -
  zones to visit, spots to place items or beacons at, quest items lying in the
  raid - each with its own icon and colour, the quest name and the distance.
- Press F7 for a six-second glance (configurable, or keep them on); the same
  peek happens automatically when a raid starts. Off-screen objectives get an
  arrow at the screen border.
- Needs the Anvil-WebOverlay library 1.8.2 or newer.

### Added

- Quest objective discovery from the local player's started quests: visit,
  zone, leave-item, place-beacon, launch-flare and find-item conditions,
  including those nested in counter conditions; hidden and completed
  conditions are skipped the way the game's own quest screen decides.
- Zone ids resolved against the raid scene's `TriggerWithId` objects (bounds
  merged per id, re-scanned when the map loads further scenes); quest items
  resolved against the world's quest loot valid for this profile.
- Per-frame projection with the game camera right before the frame renders,
  sent as one latest-only frame to an Anvil-WebOverlay click-through HUD;
  display options travel as a retained channel message.
- Markers hide while dead, in any full-screen game UI, in third person and
  while aiming through a magnified optic; the hideout and scav raids show none.
- Configuration: auto-show, toggle key, maximum distance and marker count,
  labels, distances, edge arrows, auto-hide, marker size and distance scaling.
