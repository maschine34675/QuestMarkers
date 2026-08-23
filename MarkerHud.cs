using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Text;
using UnityEngine;
using WebOverlay;

namespace QuestMarkers
{
    internal struct MarkerTarget
    {
        public string Id;
        public Vector3 Position;
        public string Label;
        public string Kind;
    }
    internal sealed class MarkerHud : IDisposable
    {
        private readonly Action<string> logWarning;
        private IWebOverlay overlay;
        private bool desiredVisible;
        private readonly StringBuilder json = new StringBuilder(4096);
        private readonly List<(MarkerTarget target, Vector3 viewport, float distance)> visible
            = new List<(MarkerTarget, Vector3, float)>();

        public bool Labels = true;
        public bool Distance = true;
        public bool EdgeIndicators = true;
        public float Scale = 1f;
        public float FarScale = 0.8f;
        public float FarDistance = 250f;
        private bool sentLabels;
        private bool sentDistance;
        private bool sentEdge;
        private float sentScale;
        private float sentFarScale;
        private float sentFarDistance;

        public MarkerHud(Action<string> logWarning)
        {
            this.logWarning = logWarning;
        }

        public bool IsVisible => desiredVisible && overlay != null;

        public void Show()
        {
            if (!WebOverlayPlugin.IsDisplayModeSupported)
            {
                logWarning("Exclusive fullscreen cannot show an overlay; use borderless windowed.");
                desiredVisible = false;
                return;
            }

            desiredVisible = true;
            if (overlay != null)
            {
                overlay.Show();
                pushConfig();
                return;
            }

            overlay = WebOverlays.Create("Quest markers", new OverlayOptions
            {
                Transparent = true,
                Dispatch = EventDispatch.MainThread,
                InjectTheme = true,
            });
            if (overlay == null)
            {
                logWarning("Overlays are unavailable - is the WebView2 runtime installed?");
                desiredVisible = false;
                return;
            }

            var created = overlay;
            overlay.Failed += () =>
            {
                logWarning(describeFailure(created));
                created.Dispose();
                if (ReferenceEquals(overlay, created))
                {
                    overlay = null;
                    desiredVisible = false;
                }
            };
            created.LoadHtml(loadPage());
            pushConfig();
        }

        public void Hide()
        {
            desiredVisible = false;
            IWebOverlay target = overlay;
            if (target == null)
                return;
            target.Post("markers", "[]");
            target.Hide();
        }
        private static string describeFailure(IWebOverlay overlay)
        {
            switch (overlay.Failure)
            {
                case OverlayFailure.RuntimeMissing:
                    return "The marker HUD needs the Microsoft WebView2 runtime - install it and restart the game.";
                case OverlayFailure.LibraryIncomplete:
                    return "The Anvil-WebOverlay installation is incomplete - reinstall its zip.";
                case OverlayFailure.RendererCrashed:
                    return "The marker HUD's browser died; press the toggle key to reopen it.";
                default:
                    return "The marker HUD failed: " + (overlay.FailureMessage ?? "see the WebOverlay log lines above.");
            }
        }
        public void Push(Camera camera, IReadOnlyList<MarkerTarget> targets, int maxMarkers, float maxDistance)
        {
            IWebOverlay target = overlay;
            if (target == null || !desiredVisible)
                return;
            if (!target.IsPageLoaded)
                return;
            if (camera == null)
            {
                target.Post("markers", "[]", PostOptions.LatestOnly);
                return;
            }
            if (Labels != sentLabels || Distance != sentDistance || EdgeIndicators != sentEdge
                || Scale != sentScale || FarScale != sentFarScale || FarDistance != sentFarDistance)
                pushConfig();

            Vector3 origin = camera.transform.position;
            visible.Clear();
            for (int i = 0; i < targets.Count; i++)
            {
                float distance = Vector3.Distance(origin, targets[i].Position);
                if (maxDistance > 0f && distance > maxDistance)
                    continue;
                Vector3 viewport = camera.WorldToViewportPoint(targets[i].Position);
                if (!float.IsFinite(viewport.x) || !float.IsFinite(viewport.y)
                    || !float.IsFinite(viewport.z) || !float.IsFinite(distance))
                    continue;
                visible.Add((targets[i], viewport, distance));
            }
            visible.Sort((a, b) => a.distance.CompareTo(b.distance));
            int count = Mathf.Min(visible.Count, maxMarkers);

            json.Length = 0;
            json.Append('[');
            for (int i = 0; i < count; i++)
            {
                (MarkerTarget item, Vector3 viewport, float distance) = visible[i];
                float x = viewport.x;
                float y = 1f - viewport.y;
                bool behind = viewport.z < 0f;
                if (behind)
                {
                    x = 1f - x;
                    y = 1f - y;
                }
                if (i > 0)
                    json.Append(',');
                json.Append("{\"i\":\"").Append(item.Id)
                    .Append("\",\"x\":").Append(x.ToString("F4", CultureInfo.InvariantCulture))
                    .Append(",\"y\":").Append(y.ToString("F4", CultureInfo.InvariantCulture))
                    .Append(",\"b\":").Append(behind ? '1' : '0')
                    .Append(",\"d\":").Append((int)distance)
                    .Append(",\"k\":\"").Append(item.Kind)
                    .Append("\",\"l\":\"").Append(item.Label)
                    .Append("\"}");
            }
            json.Append(']');
            target.Post("markers", json.ToString(), PostOptions.LatestOnly);
        }

        private void pushConfig()
        {
            IWebOverlay target = overlay;
            if (target == null)
                return;
            sentLabels = Labels;
            sentDistance = Distance;
            sentEdge = EdgeIndicators;
            sentScale = Scale;
            sentFarScale = FarScale;
            sentFarDistance = FarDistance;
            target.Post("cfg", "{\"labels\":" + (Labels ? "true" : "false")
                + ",\"distance\":" + (Distance ? "true" : "false")
                + ",\"edge\":" + (EdgeIndicators ? "true" : "false")
                + ",\"scale\":" + Scale.ToString("F2", CultureInfo.InvariantCulture)
                + ",\"farScale\":" + FarScale.ToString("F2", CultureInfo.InvariantCulture)
                + ",\"farDistance\":" + FarDistance.ToString("F1", CultureInfo.InvariantCulture) + "}",
                PostOptions.Retain);
        }

        private static string loadPage()
        {
            using (Stream stream = typeof(MarkerHud).Assembly.GetManifestResourceStream("QuestMarkers.markers.html"))
            {
                if (stream == null)
                    throw new FileNotFoundException("embedded resource QuestMarkers.markers.html");
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                    return reader.ReadToEnd();
            }
        }

        public void Dispose()
        {
            overlay?.Dispose();
            overlay = null;
            desiredVisible = false;
        }
    }
}
