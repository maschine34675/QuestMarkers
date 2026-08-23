using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace QuestMarkers
{
    [BepInPlugin("com.maschine.QuestMarkers", "maschine-QuestMarkers", "1.0.0")]
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
                "Show the markers whenever a raid starts. The toggle key works either way.");
            toggleKey = this.Config.Bind("General", "Toggle key", new KeyboardShortcut(KeyCode.F7),
                "Shows or hides the quest markers during a raid.");
            maxDistance = this.Config.Bind("Filter", "Maximum distance", 0f,
                new ConfigDescription("Hide markers farther away than this many meters. 0 shows everything.",
                    new AcceptableValueRange<float>(0f, 2000f)));
            maxMarkers = this.Config.Bind("Filter", "Maximum markers", 16,
                new ConfigDescription("At most this many markers at once; the closest objectives win.",
                    new AcceptableValueRange<int>(1, 50)));
            showLabels = this.Config.Bind("Display", "Show quest names", true,
                "Print the quest name under each marker.");
            showDistance = this.Config.Bind("Display", "Show distances", true,
                "Print the distance under each marker.");
            edgeIndicators = this.Config.Bind("Display", "Edge arrows", true,
                "Point at off-screen objectives with arrows at the screen border.");
            autoHideSeconds = this.Config.Bind("Display", "Auto-hide after", 6f,
                new ConfigDescription("Hide the markers again this many seconds after they appeared, so they stay a quick glance rather than permanent clutter. 0 keeps them visible until toggled off.",
                    new AcceptableValueRange<float>(0f, 60f)));
            markerScale = this.Config.Bind("Display", "Marker size", 1.8f,
                new ConfigDescription("Overall size of the markers.",
                    new AcceptableValueRange<float>(0.5f, 2f)));
            farScale = this.Config.Bind("Display", "Size at distance", 0.5f,
                new ConfigDescription("How large a far-away marker is compared to a close one. 1 keeps every marker the same size; 0.5 halves it at the distance below.",
                    new AcceptableValueRange<float>(0.4f, 1f)));
            farDistance = this.Config.Bind("Display", "Distance for smallest size", 250f,
                new ConfigDescription("The distance in meters at which a marker has shrunk to 'Size at distance'.",
                    new AcceptableValueRange<float>(25f, 1000f)));

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

            if (isPressed(toggleKey.Value))
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
                scanner.Refresh();
            }

            hud.Labels = showLabels.Value;
            hud.Distance = showDistance.Value;
            hud.EdgeIndicators = edgeIndicators.Value;
            hud.Scale = markerScale.Value;
            hud.FarScale = farScale.Value;
            hud.FarDistance = farDistance.Value;
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
