using System;
using System.Collections.Generic;
using System.Text;
using Comfort.Common;
using EFT;
using EFT.CameraControl;
using EFT.Interactive;
using EFT.Quests;
using EFT.UI.Screens;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QuestMarkers
{
    internal sealed class QuestTargetScanner
    {
        private readonly Action<string> logDebug;
        private readonly Action<string> logWarning;
        private readonly List<MarkerTarget> targets = new List<MarkerTarget>();
        private readonly Dictionary<string, Vector3> zonesById = new Dictionary<string, Vector3>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> wantedItems = new Dictionary<string, string>(StringComparer.Ordinal);
        private bool zonesScanned;
        private int zoneScanAttempts;
        private int scannedSceneCount = -1;
        private readonly HashSet<string> reportedCategoryConditions = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<(string name, int index)> questExits = new List<(string, int)>();
        private bool warnedThisRaid;
        public bool ShowQuests = true;
        public bool ShowExtracts;

        public QuestTargetScanner(Action<string> logDebug, Action<string> logWarning)
        {
            this.logDebug = logDebug;
            this.logWarning = logWarning;
        }

        public IReadOnlyList<MarkerTarget> Targets => targets;
        public bool InRaid
        {
            get
            {
                if (!Singleton<GameWorld>.Instantiated)
                    return false;
                GameWorld world = Singleton<GameWorld>.Instance;
                if (world == null || world is HideoutGameWorld)
                    return false;
                AbstractGame game = Singleton<AbstractGame>.Instance;
                if (game == null || game.Status != GameStatus.Started)
                    return false;
                Player player = world.MainPlayer;
                return player != null && player.IsYourPlayer;
            }
        }
        public Camera Camera => CameraManager.Exist ? CameraManager.Instance.Camera : null;
        public bool InBattleUi
        {
            get
            {
                try
                {
                    return EftScreenManager.Instance.CheckCurrentScreen(EEftScreenType.BattleUI);
                }
                catch
                {
                    return false;
                }
            }
        }
        public bool ShouldShowMarkers
        {
            get
            {
                Player player = Singleton<GameWorld>.Instantiated ? Singleton<GameWorld>.Instance.MainPlayer : null;
                if (player == null || player.PointOfView != EPointOfView.FirstPerson)
                    return false;
                try
                {
                    if (player.HealthController == null || !player.HealthController.IsAlive)
                        return false;
                    if (!InBattleUi)
                        return false;
                    if (player.HandsController is Player.FirearmController firearms
                        && firearms.IsAiming
                        && player.ProceduralWeaponAnimation?.CurrentScope?.IsOptic == true)
                        return false;
                }
                catch
                {
                    return false;
                }
                return true;
            }
        }

        public void Clear()
        {
            targets.Clear();
            zonesById.Clear();
            wantedItems.Clear();
            zonesScanned = false;
            zoneScanAttempts = 0;
            scannedSceneCount = -1;
            reportedCategoryConditions.Clear();
            warnedThisRaid = false;
        }
        public void Refresh()
        {
            targets.Clear();
            wantedItems.Clear();
            questExits.Clear();
            try
            {
                GameWorld world = Singleton<GameWorld>.Instance;
                Player player = world?.MainPlayer;
                if (player == null)
                    return;
                if (ShowQuests && player.Side != EPlayerSide.Savage)
                {
                    addQuestObjectives(player);
                    addQuestItems(world, player);
                }
                if (ShowExtracts)
                    addExtracts(world, player);
            }
            catch (Exception ex)
            {
                targets.Clear();
                if (!warnedThisRaid)
                {
                    warnedThisRaid = true;
                    logWarning("Quest scan failed this raid (" + ex.GetType().Name + ": " + ex.Message + ").");
                }
            }
        }

        private void addQuestObjectives(Player player)
        {
            QuestController questController = player.QuestController as QuestController;
            if (questController?.Quests == null)
                return;

            if (!zonesScanned || SceneManager.sceneCount != scannedSceneCount)
                scanZones();

            foreach (Quest quest in questController.Quests)
            {
                if (quest?.QuestStatus != EQuestStatus.Started
                    && quest?.QuestStatus != EQuestStatus.AvailableForFinish)
                    continue;
                if (quest.Conditions == null
                    || !quest.Conditions.TryGetValue(EQuestStatus.AvailableForFinish, out ConditionCollection conditions)
                    || conditions == null)
                    continue;
                string label = escape(quest.Template?.Name ?? "Quest");
                foreach (Condition condition in conditions)
                    addCondition(quest, condition, label);
            }
        }

        private void addCondition(Quest quest, Condition condition, string label)
        {
            if (condition == null)
                return;
            try
            {
                if (!quest.CheckVisibilityStatus(condition))
                    return;
                if (isDone(quest, condition))
                    return;

                switch (condition)
                {
                    case ConditionLeaveItemAtLocation leave:
                        addZone(condition, leave.zoneId, label, "plant");
                        break;
                    case ConditionPlaceBeacon beacon:
                        addZone(condition, beacon.zoneId, label, "beacon");
                        break;
                    case ConditionZone zone:
                        addZone(condition, zone.zoneId, label, "zone");
                        break;
                    case ConditionLaunchFlare flare:
                        addZone(condition, flare.zoneID, label, "flare");
                        break;
                    case ConditionVisitPlace visit:
                        addZone(condition, visit.target, label, "zone");
                        break;
                    case ConditionInZone inZone:
                        if (inZone.zoneIds != null)
                            foreach (string id in inZone.zoneIds)
                                addZone(condition, id, label, "zone");
                        break;
                    case ConditionExitName exit:
                        addExit(condition, exit.exitName, label);
                        break;
                    case ConditionFindItem find:
                        if (find.target == null)
                            break;
                        if (find.TargetIsCategory)
                        {
                            if (reportedCategoryConditions.Add(condition.id.ToString()))
                                logDebug("'" + label + "' wants an item category; no world marker for that.");
                            break;
                        }
                        foreach (string template in find.target)
                        {
                            if (string.IsNullOrEmpty(template))
                                continue;
                            if (wantedItems.TryGetValue(template, out string existing))
                            {
                                if (existing != label && !existing.Contains(label))
                                    wantedItems[template] = existing + " / " + label;
                            }
                            else
                            {
                                wantedItems.Add(template, label);
                            }
                        }
                        break;
                    case ConditionCounterCreator counter:
                        if (counter.Conditions != null)
                            foreach (Condition nested in counter.Conditions)
                                addCondition(quest, nested, label);
                        break;
                }
            }
            catch (Exception ex)
            {
                logDebug("Skipped a condition of '" + label + "': " + ex.Message);
            }
        }

        private bool isDone(Quest quest, Condition condition)
        {
            if (condition.IsNecessary && quest.CompletedConditions != null
                && !quest.CompletedConditions.Contains(condition.id))
                return false;
            try
            {
                return quest.IsConditionDone(condition);
            }
            catch
            {
                return false;
            }
        }

        private void addZone(Condition condition, string zoneId, string label, string kind)
        {
            if (string.IsNullOrEmpty(zoneId))
                return;
            if (!zonesById.TryGetValue(zoneId, out Vector3 position))
                return;
            targets.Add(new MarkerTarget
            {
                Id = escape("z:" + condition.id + ":" + zoneId),
                Position = position,
                Label = label,
                Kind = escape(kind),
            });
        }
        private void addExit(Condition condition, string exitName, string label)
        {
            if (string.IsNullOrEmpty(exitName))
                return;
            var controller = Singleton<GameWorld>.Instance?.ExfiltrationController;
            if (controller?.ExfiltrationPoints == null)
                return;
            foreach (ExfiltrationPoint point in controller.ExfiltrationPoints)
            {
                if (point == null || point.Settings == null || point.Settings.Name != exitName)
                    continue;
                questExits.Add((exitName, targets.Count));
                targets.Add(new MarkerTarget
                {
                    Id = escape("x:" + condition.id + ":" + exitName),
                    Position = boundsFor(point).center,
                    Label = label,
                    Kind = "exit",
                });
            }
        }
        private void addExtracts(GameWorld world, Player player)
        {
            var controller = world.ExfiltrationController;
            if (controller == null || player.Profile == null)
                return;
            if (controller.IsMyPlayerBanned())
                return;
            if (player.Side == EPlayerSide.Savage)
            {
                addScavExtracts(controller.ScavExfiltrationPoints,
                    controller.GetScavSecretExits(), player.ProfileId);
                return;
            }
            ExfiltrationPoint[] eligible = controller.EligiblePoints(player.Profile);
            if (eligible != null)
                foreach (ExfiltrationPoint point in eligible)
                    addExtract(point);
            var secret = controller.SecretEligiblePoints();
            if (secret != null)
                foreach (ExfiltrationPoint point in secret)
                    addExtract(point);
        }
        private void addScavExtracts(ScavExfiltrationPoint[] claimed, ExfiltrationPoint[] secret, string profileId)
        {
            if (claimed != null)
            {
                foreach (ScavExfiltrationPoint point in claimed)
                {
                    if (point == null || point.EligibleIds == null
                        || !point.EligibleIds.Contains(profileId))
                        continue;
                    addExtract(point);
                }
            }
            if (secret == null)
                return;
            foreach (ExfiltrationPoint point in secret)
                addExtract(point);
        }

        private void addExtract(ExfiltrationPoint point)
        {
            if (point == null || point.Settings == null || string.IsNullOrEmpty(point.Settings.Name))
                return;
            string state = stateOf(point);
            if (state == null)
                return;
            string name = point.Settings.Name;
            bool lent = false;
            for (int i = 0; i < questExits.Count; i++)
            {
                if (questExits[i].name != name)
                    continue;
                MarkerTarget quest = targets[questExits[i].index];
                quest.State = state;
                targets[questExits[i].index] = quest;
                lent = true;
            }
            if (lent)
                return;
            targets.Add(new MarkerTarget
            {
                Id = escape("e:" + name),
                Position = boundsFor(point).center,
                Label = escape(name.Localized()),
                Kind = "extract",
                State = state,
            });
        }
        private static string stateOf(ExfiltrationPoint point)
        {
            EExfiltrationStatus status = point.Status;
            if (status == EExfiltrationStatus.Countdown)
                return "countdown";
            if (isRolled(point) && point.Settings.Chance < 100f)
                return "locked";
            switch (status)
            {
                case EExfiltrationStatus.RegularMode:
                case EExfiltrationStatus.AwaitsManualActivation:
                    return needsScav(point) ? "locked" : "open";
                case EExfiltrationStatus.UncompleteRequirements:
                case EExfiltrationStatus.Pending:
                    return "locked";
                default:
                    return null;
            }
        }
        private static bool isRolled(ExfiltrationPoint point)
        {
            return !(point is ScavExfiltrationPoint) || point is SharedExfiltrationPoint;
        }

        private static bool needsScav(ExfiltrationPoint point)
        {
            ExfiltrationRequirement[] requirements = point.Requirements;
            if (requirements == null)
                return false;
            foreach (ExfiltrationRequirement requirement in requirements)
                if (requirement != null && requirement.Requirement == ERequirementState.ScavCooperation)
                    return true;
            return false;
        }

        private void addQuestItems(GameWorld world, Player player)
        {
            if (wantedItems.Count == 0 || world.LootItems == null)
                return;
            string profileId = player.ProfileId;
            for (int i = 0; i < world.LootItems.Count; i++)
            {
                LootItem loot = world.LootItems.GetByIndex(i);
                if (loot == null || loot.Item == null || !loot.Item.QuestItem)
                    continue;
                if (!loot.IsValidForProfile(profileId))
                    continue;
                if (loot.TemplateId == null || !wantedItems.TryGetValue(loot.TemplateId, out string label))
                    continue;
                targets.Add(new MarkerTarget
                {
                    Id = escape("i:" + loot.ItemId),
                    Position = loot.transform.position,
                    Label = label,
                    Kind = "item",
                });
            }
        }
        private void scanZones()
        {
            var boundsById = new Dictionary<string, Bounds>(StringComparer.Ordinal);
            foreach (TriggerWithId trigger in UnityEngine.Object.FindObjectsOfType<TriggerWithId>(true))
            {
                if (trigger == null || string.IsNullOrEmpty(trigger.Id))
                    continue;
                Bounds bounds = boundsFor(trigger);
                if (boundsById.TryGetValue(trigger.Id, out Bounds known))
                {
                    known.Encapsulate(bounds);
                    boundsById[trigger.Id] = known;
                }
                else
                {
                    boundsById[trigger.Id] = bounds;
                }
            }
            zonesById.Clear();
            foreach (KeyValuePair<string, Bounds> pair in boundsById)
                zonesById.Add(pair.Key, pair.Value.center);
            scannedSceneCount = SceneManager.sceneCount;
            zoneScanAttempts++;
            zonesScanned = zonesById.Count > 0 || zoneScanAttempts >= 10;
            logDebug("Zone scan found " + zonesById.Count + " trigger ids in "
                + scannedSceneCount + " scenes (attempt " + zoneScanAttempts + ").");
        }
        private static string escape(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            bool clean = true;
            foreach (char c in text)
            {
                if (c == '"' || c == '\\' || c < ' ')
                {
                    clean = false;
                    break;
                }
            }
            if (clean)
                return text;
            var sb = new StringBuilder(text.Length + 8);
            foreach (char c in text)
            {
                if (c == '"' || c == '\\')
                    sb.Append('\\').Append(c);
                else if (c < ' ')
                    sb.Append(' ');
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }

        private static Bounds boundsFor(Component trigger)
        {
            var bounds = new Bounds(trigger.transform.position, Vector3.zero);
            if (!trigger.gameObject.activeInHierarchy)
                return bounds;
            foreach (Collider collider in trigger.GetComponents<Collider>())
                if (collider != null && collider.enabled)
                    bounds.Encapsulate(collider.bounds);
            return bounds;
        }
    }
}
