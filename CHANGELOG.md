# Changelog

## [Unreleased]

## [1.4.0]

### Forge version notes

- Airdrops and the BTR get markers too, and they follow them live: a cyan
  parachute rides with each airdrop crate from the moment it leaves the plane
  until it has landed and beyond, and a red armoured car with the BTR while
  it is out on its route on Woods and Streets. Both are round, a third shape
  next to the quest pins and the extract diamonds.
- A second key, and a checklist per key: in the F12 settings menu you tick
  what each key shows - Quests, Extracts, Airdrops, BTR. The toggle key shows
  all four; the second key is not bound out of the box and shows the
  extracts alone. Pressing the other key while markers are up switches to
  its list, and the same key again hides them. The four on/off switches
  under *Filter* are gone - whatever you had switched off there is carried
  over into the toggle key's list the first time the game starts.

### Added

- `MarkerSet`, a `[Flags]` enum, as the type of a key's list: the
  ConfigurationManager SPT ships draws a flags enum as one checkbox per
  member, labelled with the member's name, and BepInEx stores it as a
  readable list such as `Extracts, Airdrops`. Two keys, each with its own
  list (`Toggle key shows`, `Second key shows`); the key that started a peek
  decides which list the scan reads, afresh on every scan, so a change in
  F12 counts at once. An unbound key never fires. When both keys fire at
  once - one extends the other with a modifier, such as I and Ctrl+I - the
  one with more modifiers wins, since a key press still counts while other
  keys are held (the toggle has to work while sprinting).
- A one-time takeover of the old switches. BepInEx keeps the lines of a
  file that no Bind has claimed in a private `OrphanedEntries` dictionary and
  writes them back on every save; it is read by reflection before anything
  is bound. On the start that first creates the toggle key's list, each old
  switch that was off clears its member from that list; either way the old
  lines are removed from the file. The list and the takeover are written in
  one save with `SaveOnConfigSet` off, so a crash in between cannot leave the
  plain default on disk and make the next start skip the takeover. Should
  the property ever be missing, the takeover is simply skipped.
- Moving targets. A marker can now carry a transform instead of a fixed
  position, and the per-frame projection reads it every frame, a set height
  above the object. A target whose object the game has deactivated is
  skipped the same frame; the game parks rather than destroys both kinds -
  a crate goes back to its pool, the BTR to its depot - so Unity's null test
  alone would never fire.
- Airdrop crates from `GameWorld.SynchronizableObjectLogicProcessor
  .GetSynchronizableObjects()`, the registry the game keeps of its
  synchronised objects: `AirdropSynchronizableObject` entries that are
  `IsActive` and `IsInited`. A scene search would also find the crates kept
  pooled for later drops, and a crate joins the registry before it is placed.
  The crate falls and lands as one object, so one transform covers the whole
  descent.
- The BTR from `GameWorld.BtrController.BtrView`, marked only while its
  `RouteState` is `OnRoute`: the view exists all raid long, parked at the
  depot before it sets out, for the whole raid when the spawn roll kept it
  there, and after its route is done. Hidden while the player is boarding,
  riding or leaving it.
- Round markers for both. A circle has no tip, and inscribed in the rotated
  pin box it would float 5.4 px (times the marker size) above the spot the
  other shapes point at; the page moves it down the box's diagonal by that
  much before scaling, so it sits on the spot at every size.
- Both types are read through `var` where their containers are named
  differently on the 4.0 line; the crate needs a reference to
  `Sirenix.Serialization`, where its base class lives.

### Changed

- The four Filter switches (`Show quest markers`, `Show my extracts`,
  `Show airdrops`, `Show the BTR`) are replaced by the key lists; the two
  that shipped in 1.3.0 are taken over as described above.
- The marker cap applies to targets flagged as quest objectives rather than
  to everything without an extract state, so airdrops and the BTR stay out of
  it like the extracts do.

## [1.3.0]

### Forge version notes

- Your own extracts get markers too - the same list the game shows on the
  exfil panel, now placed in the world. They are diamonds rather than pins,
  so they never pass for a quest objective, and their colour tells the
  state: green when open, amber and pulsing while a countdown runs, grey
  while not confirmed open - a switch, a car or a train still outstanding,
  or an exit left to chance, whose roll the markers never give away. Secret
  exits appear only once you have found them. *Show my extracts* turns them
  off.
- An extract a quest sends you to keeps its rose quest pin and now shows the
  same state on it. Extracts never count towards *Maximum markers*.
- *Show quest markers* turns the quest objectives off, for players who want
  the extract markers alone.
- Scav raids get extract markers too - the exits that raid claimed for you,
  exactly the ones its panel lists. Quest markers stay away there, as a scav
  carries no PMC quest log.
- The default key is now **I**, the key the Quest Tracker mod uses for its
  panel, so one press can give you the task list and the markers together. An
  existing installation keeps whatever key its configuration file already
  holds; change it there or in the settings menu.
- The F12 settings menu shows readable option names in a deliberate order
  rather than the raw configuration keys.

### Added

- An extract layer built on the game's own answers rather than on rules of
  its own: `ExfiltrationController.EligiblePoints(Profile)` and
  `SecretEligiblePoints()` give the exits this raid assigns the player, and
  each point's live `Status` gives the state - `RegularMode` and
  `AwaitsManualActivation` (a boarding train, a manual exit ready to use)
  open, `Countdown` counting down, `UncompleteRequirements` and `Pending`
  locked, `NotPresent` and `Hidden` not shown. Per-player item conditions
  such as paracord never reach that status, exactly as in vanilla.
- Where the status says more than the exfil panel, the markers hold back:
  an exit with a chance below 100 % shows locked unless it counts down, a
  failed roll included, because the panel shows "??" until the player has
  stood in it; the co-op exit starts out `RegularMode` but only counts as
  met in the countdown a scav and a PMC start together, so it shows locked
  until then. While the player is committed to a transit
  (`IsMyPlayerBanned()`), no extract is shown.
- A diamond marker kind with a double-chevron icon, a state colour on marker
  and edge arrow, a dashed outline while locked and a glow that pulses through
  box-shadow while counting down.
- Scav extracts from the list the game claims for the raid: the points in
  `ScavExfiltrationPoints` whose `EligibleIds` hold this profile, plus
  `GetScavSecretExits()`. The scav path must never go through
  `EligiblePoints`, which SPT patches into re-running that claim - it would
  redraw the player's exits every scan and disable the ones nobody else
  holds. The claimed ids are the whole filter: what a scav may not use is
  exactly what their id is missing from, and an exit the game itself closed
  says so in its status.
- The chance rule now applies only to points whose settings the game loads
  and rolls. A scav-only exit never goes through that pass, so its chance is
  scene data the game ignores, and reading it would have greyed out exits the
  raid had handed the player.
- *Show quest markers* (on by default) gates the whole quest walk, the scene
  trigger scan included, so an extracts-only setup does no quest work at all.
  With the quest markers off, an exit a quest names is simply one of the
  extracts and gets the diamond.

### Changed

- The toggle key's default moves from F7 to I. F7 was picked to sit next to
  Anvil-WebOverlay's optional demo overlay; I pairs the markers with the Quest
  Tracker mod instead, which is the more useful neighbour. Only fresh
  installations see the new default - BepInEx keeps the value already written
  in the configuration file.
- The toggle key is only read while the raid interface has the keyboard. A
  letter reaches the game's own fields, so typing an I into a search box with
  the inventory open would otherwise have flipped the markers behind it -
  invisibly, since nothing is drawn over a full-screen interface anyway.
- Every option carries a settings-menu name and position through a duck-typed
  `ConfigurationManagerAttributes` tag, so the F12 menu reads as a menu:
  sections in binding order, entries by descending order within them. The
  configuration keys and sections are untouched, so existing files stay valid.
- The Forge teaser names the extracts as well, and no longer advertises a key
  that has changed twice.
- The stated Anvil-WebOverlay floor is corrected to 1.11.0, the version this
  build compiles against and therefore the minimum BepInEx enforces: the
  dependency attribute takes the library's own build-time version, so the
  floor moved with 1.2.0 already while its notes and the README still said
  1.10.0. A player on 1.10.0 would have seen the plugin skipped with only a
  log line.
- *Maximum markers* counts quest markers only. Extracts - a quest exit that
  is one of the player's extracts included - are few and usually far away,
  and under a shared cap nearby quest pins would push out the very markers a
  player pressed the key to find. *Maximum distance* still applies to them.
- An exit a quest already points at keeps its quest pin, which carries the
  quest name, and takes the extract state on it - dashed while locked,
  pulsing while counting down - instead of a second marker on the same spot.

## [1.2.0]

### Forge version notes

- The markers are now labelled in Chakra Petch, a squarish technical face
  that suits the game better than the Windows interface font. It comes with
  the mod, so nothing needs installing; *Use the bundled font* turns it off
  again.

### Added

- Chakra Petch bundled under the SIL Open Font License 1.1 and used for the
  marker labels by default. The archive stays a single DLL: the font, its
  full licence text and the notice all live inside the assembly - the notice
  in the copyright field Windows shows under Properties, the licence as the
  embedded resource `QuestMarkers.font-license.txt`, and the plugin names the
  font in the log at startup. The OFL allows exactly this as long as the
  metadata is easy for a user to view. `web/FONT-SOURCES.md` records the
  file's origin, upstream commit and checksum.

### Changed

- The README and the Forge description name driver-level frame generation
  (AMD Fluid Motion Frames, Lossless Scaling, NVIDIA Smooth Motion) as a
  known limitation: any window over the game changes how Windows presents
  its frames, and the generator's pacing suffers while the markers are up.
  From a player report on AMD Fluid Motion Frames 2.1; nothing in the mod
  changes, and the mechanism is written up in Anvil-WebOverlay's
  troubleshooting guide.

## [1.1.0]

### Forge version notes

- Quests that ask you to leave through a particular extract now get a marker
  there too, with its own rose door icon. Extracts a quest does not name are
  still not shown - that is what map mods are for.

### Changed

- The library floor moves to Anvil-WebOverlay 1.10.0, the version this build
  is compiled against.

### Added

- `ConditionExitName` objectives resolve against the raid's exfiltration
  points by name and become a sixth marker kind, `exit`. A condition that
  names no exit produces no marker, since any extract satisfies it.

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
