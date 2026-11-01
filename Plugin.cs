using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace QuestMarkers
{
    [BepInPlugin("com.maschine.QuestMarkers", "maschine-QuestMarkers", "1.4.0")]
    [BepInDependency(WebOverlay.Branding.PluginGuid, WebOverlay.Branding.PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        private ConfigEntry<bool> autoShow;
        private ConfigEntry<KeyboardShortcut> toggleKey;
        private ConfigEntry<MarkerSet> toggleShows;
        private ConfigEntry<KeyboardShortcut> secondKey;
        private ConfigEntry<MarkerSet> secondShows;
        private ConfigEntry<float> maxDistance;
        private ConfigEntry<int> maxMarkers;
        private ConfigEntry<bool> showLabels;
        private ConfigEntry<bool> showDistance;
        private ConfigEntry<bool> edgeIndicators;
        private ConfigEntry<float> autoHideSeconds;
        private ConfigEntry<float> markerScale;
        private ConfigEntry<float> farScale;
        private ConfigEntry<float> farDistance;
        private ConfigEntry<bool> gameFont;

        private MarkerHud hud;
        private QuestTargetScanner scanner;
        private bool wasInRaid;
        private float nextScan;
        private float shownSeconds;
        private bool peeking;
        private bool secondKeyPeek;
        private bool clearPending;
        private int pushedFrame = -1;

        private void Awake()
        {
            Dictionary<ConfigDefinition, string> orphans = orphanedEntries(this.Config);
            bool firstStart = orphans != null && !orphans.ContainsKey(toggleShowsDefinition);

            toggleKey = this.Config.Bind("General", "Toggle key", new KeyboardShortcut(KeyCode.I),
                Tagged("Toggle Key", 50,
                    "Shows or hides the markers ticked below during a raid. I is also the Quest Tracker mod's panel key, so one press can do both."));
            bool tookOver;
            this.Config.SaveOnConfigSet = false;
            try
            {
                toggleShows = this.Config.Bind(toggleShowsDefinition,
                    MarkerSet.Quests | MarkerSet.Extracts | MarkerSet.Airdrops | MarkerSet.BTR,
                    Tagged("Toggle Key Shows", 40,
                        "What the toggle key marks, and what appears on its own when a raid starts. Quests: your open objectives. Extracts: the exits this raid gives you, with their state. Airdrops: crates, followed live. BTR: the BTR while it is out on its route."));
                tookOver = takeOverOldSwitches(orphans, firstStart);
            }
            finally
            {
                this.Config.SaveOnConfigSet = true;
            }
            if (firstStart || tookOver)
                this.Config.Save();
            secondKey = this.Config.Bind("General", "Second key", KeyboardShortcut.Empty,
                Tagged("Second Key", 30,
                    "A second key with its own list below - out of the box the extracts alone. Not bound until you pick a key. Pressing the other key while markers are up switches to its list; the same key again hides them."));
            secondShows = this.Config.Bind("General", "Second key shows", MarkerSet.Extracts,
                Tagged("Second Key Shows", 20,
                    "What the second key marks."));
            autoShow = this.Config.Bind("General", "Show in raid automatically", true,
                Tagged("Show At Raid Start", 10,
                    "Show the toggle key's markers whenever a raid starts. Both keys work either way."));
            maxDistance = this.Config.Bind("Filter", "Maximum distance", 0f,
                Tagged("Maximum Distance (m)", 10,
                    "Hide markers farther away than this many meters, extracts included. 0 shows everything.",
                    new AcceptableValueRange<float>(0f, 2000f)));
            maxMarkers = this.Config.Bind("Filter", "Maximum markers", 16,
                Tagged("Maximum Quest Markers", 20,
                    "At most this many quest markers at once; the closest objectives win. Extracts do not count towards this.",
                    new AcceptableValueRange<int>(1, 50)));
            showLabels = this.Config.Bind("Display", "Show quest names", true,
                Tagged("Show Quest Names", 80,
                    "Print the quest name under each marker."));
            showDistance = this.Config.Bind("Display", "Show distances", true,
                Tagged("Show Distances", 70,
                    "Print the distance under each marker."));
            edgeIndicators = this.Config.Bind("Display", "Edge arrows", true,
                Tagged("Arrows For Off-Screen Targets", 60,
                    "Point at off-screen objectives with arrows at the screen border."));
            autoHideSeconds = this.Config.Bind("Display", "Auto-hide after", 6f,
                Tagged("Auto-Hide After (s)", 50,
                    "Hide the markers again this many seconds after they appeared, so they stay a quick glance rather than permanent clutter. 0 keeps them visible until toggled off.",
                    new AcceptableValueRange<float>(0f, 60f)));
            markerScale = this.Config.Bind("Display", "Marker size", 1.8f,
                Tagged("Marker Size", 40,
                    "Overall size of the markers.",
                    new AcceptableValueRange<float>(0.5f, 2f)));
            farScale = this.Config.Bind("Display", "Size at distance", 0.5f,
                Tagged("Size At Distance", 30,
                    "How large a far-away marker is compared to a close one. 1 keeps every marker the same size; 0.5 halves it at the distance below.",
                    new AcceptableValueRange<float>(0.4f, 1f)));
            farDistance = this.Config.Bind("Display", "Distance for smallest size", 250f,
                Tagged("Distance For Smallest Size (m)", 20,
                    "The distance in meters at which a marker has shrunk to 'Size at distance'.",
                    new AcceptableValueRange<float>(25f, 1000f)));
            gameFont = this.Config.Bind("Display", "Use the bundled font", true,
                Tagged("Use The Bundled Font", 10,
                    "Label the markers in Chakra Petch, a squarish technical typeface that suits the game better than the Windows one. Turn this off for the plain interface font."));
            this.Logger.LogInfo("Marker labels use Chakra Petch (c) 2018 The Chakra Petch Project Authors, "
                + "SIL Open Font License 1.1 - https://scripts.sil.org/OFL");

            hud = new MarkerHud(this.Logger.LogWarning);
            scanner = new QuestTargetScanner(this.Logger.LogDebug, this.Logger.LogWarning);
            Application.onBeforeRender += project;
        }

        private void Update()
        {
            bool inRaid = scanner.InRaid;
            if (inRaid != wasInRaid)
            {
                wasInRaid = inRaid;
                if (inRaid)
                {
                    hud.Show();
                    if (autoShow.Value)
                        startPeek(false);
                }
                else
                {
                    endPeek();
                    hud.Hide();
                    scanner.Clear();
                }
            }

            if (!inRaid)
                return;
            if (scanner.InBattleUi)
            {
                bool first = isPressed(toggleKey.Value);
                bool second = isPressed(secondKey.Value);
                if (first && second)
                    press(secondKey.Value.Modifiers.Count() > toggleKey.Value.Modifiers.Count());
                else if (first)
                    press(false);
                else if (second)
                    press(true);
            }

            if (!peeking || !hud.IsVisible)
                return;
            if (autoHideSeconds.Value > 0f)
            {
                if (scanner.ShouldShowMarkers && scanner.Camera != null)
                    shownSeconds += Time.unscaledDeltaTime;
                if (shownSeconds >= autoHideSeconds.Value)
                {
                    endPeek();
                    return;
                }
            }
            if (Time.unscaledTime >= nextScan)
            {
                nextScan = Time.unscaledTime + 2f;
                MarkerSet shows = secondKeyPeek ? secondShows.Value : toggleShows.Value;
                scanner.ShowQuests = (shows & MarkerSet.Quests) != 0;
                scanner.ShowExtracts = (shows & MarkerSet.Extracts) != 0;
                scanner.ShowAirdrops = (shows & MarkerSet.Airdrops) != 0;
                scanner.ShowBtr = (shows & MarkerSet.BTR) != 0;
                scanner.Refresh();
            }

            hud.Labels = showLabels.Value;
            hud.Distance = showDistance.Value;
            hud.EdgeIndicators = edgeIndicators.Value;
            hud.Scale = markerScale.Value;
            hud.FarScale = farScale.Value;
            hud.FarDistance = farDistance.Value;
            hud.GameFont = gameFont.Value;
        }
        private void press(bool second)
        {
            if (peeking && secondKeyPeek == second)
                endPeek();
            else
                startPeek(second);
        }

        private void startPeek(bool second)
        {
            secondKeyPeek = second;
            peeking = true;
            shownSeconds = 0f;
            nextScan = 0f;
            if (!hud.IsVisible)
                hud.Show();
        }

        private void endPeek()
        {
            peeking = false;
            clearPending = true;
        }
        private void project()
        {
            if (pushedFrame == Time.frameCount || hud == null || !hud.IsVisible || !scanner.InRaid)
                return;
            if (!peeking && !clearPending)
                return;
            pushedFrame = Time.frameCount;
            clearPending = false;
            hud.Push(scanner.Camera,
                peeking && scanner.ShouldShowMarkers ? scanner.Targets : noTargets,
                maxMarkers.Value, maxDistance.Value);
        }

        private static readonly MarkerTarget[] noTargets = new MarkerTarget[0];

        private static readonly ConfigDefinition toggleShowsDefinition =
            new ConfigDefinition("General", "Toggle key shows");
        private static readonly (string key, MarkerSet shows)[] oldSwitches =
        {
            ("Show quest markers", MarkerSet.Quests),
            ("Show my extracts", MarkerSet.Extracts),
            ("Show airdrops", MarkerSet.Airdrops),
            ("Show the BTR", MarkerSet.BTR),
        };
        private bool takeOverOldSwitches(Dictionary<ConfigDefinition, string> orphans, bool firstStart)
        {
            if (orphans == null)
                return false;
            MarkerSet shows = toggleShows.Value;
            bool found = false;
            foreach ((string key, MarkerSet covered) in oldSwitches)
            {
                var definition = new ConfigDefinition("Filter", key);
                if (!orphans.TryGetValue(definition, out string text))
                    continue;
                found = true;
                orphans.Remove(definition);
                if (firstStart && bool.TryParse(text.Trim(), out bool on) && !on)
                    shows &= ~covered;
            }
            if (!found)
                return false;
            if (firstStart)
                toggleShows.Value = shows;
            this.Logger.LogInfo("Took the old Filter switches over into 'Toggle key shows': " + toggleShows.Value + ".");
            return true;
        }
        private static Dictionary<ConfigDefinition, string> orphanedEntries(ConfigFile file)
        {
            try
            {
                return typeof(ConfigFile)
                    .GetProperty("OrphanedEntries", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(file) as Dictionary<ConfigDefinition, string>;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static ConfigDescription Tagged(string displayName, int order, string description)
        {
            return Tagged(displayName, order, description, null);
        }
        private static ConfigDescription Tagged(string displayName, int order, string description,
            AcceptableValueBase acceptableValues)
        {
            return new ConfigDescription(description, acceptableValues,
                new ConfigurationManagerAttributes
                {
                    DispName = displayName,
                    Order = order,
                    ShowRangeAsPercent = false,
                });
        }
        private static bool isPressed(KeyboardShortcut shortcut)
        {
            if (shortcut.MainKey == KeyCode.None || !Input.GetKeyDown(shortcut.MainKey))
                return false;
            foreach (KeyCode modifier in shortcut.Modifiers)
                if (!Input.GetKey(modifier))
                    return false;
            return true;
        }

        private void OnDestroy()
        {
            Application.onBeforeRender -= project;
            hud?.Dispose();
        }
    }
}
