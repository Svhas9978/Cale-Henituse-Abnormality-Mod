using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace CaleHenituseAbnormality
{
    internal static class CaleStoryState
    {
        private static int lastObservedDay = -1;
        private static int resetToDayOneCount;

        private static bool manualCounterViolation;
        private static bool armorNoonCondition;

        private static int attachmentStreak;

        public static bool SecretConspiracyUnlocked { get; private set; }
        public static bool TrueLazyEndingReady { get; private set; }

        public static void InitializeRun()
        {
            if (lastObservedDay < 0)
                lastObservedDay = -1;
        }

        public static void ObserveDayBoundary()
        {
            int day = ReadCurrentDay();
            if (day < 0)
                return;

            if (lastObservedDay > 1 && day == 1)
            {
                resetToDayOneCount++;

                if (resetToDayOneCount >= 5 && !SecretConspiracyUnlocked)
                {
                    SecretConspiracyUnlocked = true;
                    CaleConspiracyOverlay.TryOpen();
                }
            }

            if (day == 1 && attachmentStreak > 0)
            {
                // A full TT2 reset clears the streak as observed by the player.
                attachmentStreak = 0;
            }

            if (day >= 45 && !manualCounterViolation)
                UpdateEndingFlag();

            lastObservedDay = day;
        }

        public static void ObserveStageEnd()
        {
            UpdateEndingFlag();
        }

        public static void RegisterAttachmentResult(CaleHenituseAbnormality cale)
        {
            // The final "excellent work" test is intentionally represented as
            // a consecutive Attachment streak. The actual work result remains
            // controlled by the vanilla UseSkill system.
            if (cale == null || cale.IsSleeping)
            {
                attachmentStreak = 0;
                return;
            }

            attachmentStreak++;

            if (attachmentStreak >= 5)
                UpdateEndingFlag();
        }

        public static void RegisterAnyInstinctWork()
        {
            // Reserved for the hidden quest "Understanding Rest".
        }

        public static void RegisterSilverShieldUse()
        {
            UpdateEndingFlag();
        }

        public static void MarkManualSuppressionAttempt()
        {
            manualCounterViolation = true;
            UpdateEndingFlag();
        }


        public static void ObserveArmorDuringNoon(CaleHenituseAbnormality cale)
        {
            if (cale == null || cale.model == null)
                return;

            var manager = OrdealManager.instance;
            var ordeals = manager != null ? manager.GetOrdealCreatureList() : Array.Empty<OrdealCreatureModel>();
            bool noon = false;

            for (int i = 0; i < ordeals.Length; i++)
            {
                var ordeal = ordeals[i];
                if (ordeal == null || ordeal.hp <= 0)
                    continue;

                if (!string.Equals(ordeal.sefiraNum, cale.model.sefiraNum, StringComparison.OrdinalIgnoreCase))
                    continue;

                string typeName = ordeal.GetType().Name;
                if (typeName.IndexOf("Noon", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    noon = true;
                    break;
                }
            }

            if (!noon)
                return;

            var workers = UnityEngine.Object.FindObjectsOfType<WorkerUnit>();
            for (int i = 0; i < workers.Length; i++)
            {
                WorkerUnit view = workers[i];
                if (view == null || view.workerModel == null)
                    continue;

                WorkerModel worker = view.workerModel;
                if (worker.hp <= 0)
                    continue;

                if (!string.Equals(worker.currentSefira, cale.model.sefiraNum, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (worker.HasEquipment(CaleHenituseAbnormality.ArmorId))
                {
                    RegisterArmorNoonCondition();
                    return;
                }
            }
        }

        public static void RegisterArmorNoonCondition()
        {
            armorNoonCondition = true;
            UpdateEndingFlag();
        }

        public static void UpdateEndingFlag()
        {
            int day = ReadCurrentDay();

            // The third condition is interpreted as "reached day 45 without a
            // manual counter violation"; the first two are tracked separately.
            TrueLazyEndingReady =
                day >= 45 &&
                attachmentStreak >= 5 &&
                armorNoonCondition &&
                !manualCounterViolation;
        }

        private static int ReadCurrentDay()
        {
            object manager = GetGlobalGameManager();
            if (manager == null)
                return -1;

            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            Type type = manager.GetType();

            string[] names = { "Day", "day", "CurrentDay", "currentDay" };

            for (int i = 0; i < names.Length; i++)
            {
                FieldInfo field = type.GetField(names[i], flags);
                if (field != null)
                {
                    try
                    {
                        object value = field.GetValue(manager);
                        if (value is int)
                            return (int)value;
                    }
                    catch
                    {
                    }
                }

                PropertyInfo property = type.GetProperty(names[i], flags);
                if (property != null && property.CanRead)
                {
                    try
                    {
                        object value = property.GetValue(manager, null);
                        if (value is int)
                            return (int)value;
                    }
                    catch
                    {
                    }
                }
            }

            return -1;
        }

        public static int GetCurrentDay()
        {
            return ReadCurrentDay();
        }

        private static object GetGlobalGameManager()
        {
            try
            {
                PropertyInfo property = typeof(GlobalGameManager).GetProperty(
                    "instance",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (property != null)
                    return property.GetValue(null, null);
            }
            catch
            {
            }

            try
            {
                FieldInfo field = typeof(GlobalGameManager).GetField(
                    "instance",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (field != null)
                    return field.GetValue(null);
            }
            catch
            {
            }

            return null;
        }
    }
}
