using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace QuestMarkers
{
    [BepInPlugin("com.maschine.QuestMarkers", "maschine-QuestMarkers", "1.3.0")]
    [BepInDependency(WebOverlay.Branding.PluginGuid, WebOverlay.Branding.PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        private ConfigEntry<bool> autoShow;
        private ConfigEntry<KeyboardShortcut> toggleKey;
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
        private ConfigEntry<bool> showQuests;
        private ConfigEntry<bool> showExtracts;

        private MarkerHud hud;
        private QuestTargetScanner scanner;
        private bool wasInRaid;
        private float nextScan;
        private float shownSeconds;
        private bool peeking;
        private bool clearPending;
        private int pushedFrame = -1;

        private void Awake()
        {
            autoShow = this.Config.Bind("General", "Show in raid automatically", true,
                Tagged("Show At Raid Start", 10,
                    "Show the markers whenever a raid starts. The toggle key works either way."));
            toggleKey = this.Config.Bind("General", "Toggle key", new KeyboardShortcut(KeyCode.I),
                Tagged("Toggle Key", 20,
                    "Shows or hides the markers during a raid. I is also the Quest Tracker mod's panel key, so one press can do both."));
            maxDistance = this.Config.Bind("Filter", "Maximum distance", 0f,
                Tagged("Maximum Distance (m)", 10,
                    "Hide markers farther away than this many meters, extracts included. 0 shows everything.",
                    new AcceptableValueRange<float>(0f, 2000f)));
            maxMarkers = this.Config.Bind("Filter", "Maximum markers", 16,
                Tagged("Maximum Quest Markers", 20,
                    "At most this many quest markers at once; the closest objectives win. Extracts do not count towards this.",
                    new AcceptableValueRange<int>(1, 50)));
            showQuests = this.Config.Bind("Filter", "Show quest markers", true,
                Tagged("Show Quest Markers", 40,
                    "Mark the objectives of your started quests. Turn this off to keep only the extract markers."));
            showExtracts = this.Config.Bind("Filter", "Show my extracts", true,
                Tagged("Show My Extracts", 30,
                    "Also mark the extracts this raid gives you - the same list the game shows on the exfil panel, placed in the world. Green when open, amber and pulsing while a countdown runs, grey while not confirmed open: a switch, a car or a train still outstanding, or an exit left to chance, whose roll the markers never give away. Secret exits appear only once you have found them. As a scav you get the exits that raid claimed for you instead."));
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
                        startPeek();
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
            if (scanner.InBattleUi && isPressed(toggleKey.Value))
            {
                if (peeking)
                    endPeek();
                else
                    startPeek();
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
                scanner.ShowQuests = showQuests.Value;
                scanner.ShowExtracts = showExtracts.Value;
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

        private void startPeek()
        {
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
            if (!Input.GetKeyDown(shortcut.MainKey))
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
