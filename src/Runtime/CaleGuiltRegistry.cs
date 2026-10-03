using System;
using System.Collections.Generic;
using System.Reflection;
using Harmony;

namespace CaleHenituseAbnormality
{
    internal static class CaleGuiltRegistry
    {
        private sealed class Entry
        {
            public float Multiplier;
            public int RemainingStages;
        }

        private static readonly Dictionary<long, Entry> Entries = new Dictionary<long, Entry>();

        public static void Apply(WorkerModel worker, int workPenalty, float lifetime)
        {
            if (worker == null)
                return;

            Entries[worker.instanceId] = new Entry
            {
                Multiplier = 0.85f,
                RemainingStages = Math.Max(1, (int)Math.Ceiling(lifetime))
            };
        }

        public static bool TryGet(WorkerModel worker, out float multiplier)
        {
            multiplier = 1f;

            if (worker == null)
                return false;

            Entry entry;
            if (!Entries.TryGetValue(worker.instanceId, out entry))
                return false;

            multiplier = entry.Multiplier;
            return true;
        }

        public static void Clear()
        {
            Entries.Clear();
        }

        public static void ClearFor(long instanceId)
        {
            Entries.Remove(instanceId);
        }
    }

    [HarmonyPatch(typeof(UnitModel), "GetCubeSpeedBuf")]
    internal static class CaleGuiltWorkSpeedPatch
    {
        [HarmonyPostfix]
        private static void Postfix(UnitModel __instance, ref int __result)
        {
            WorkerModel worker = __instance as WorkerModel;
            float mult;

            if (worker == null || !CaleGuiltRegistry.TryGet(worker, out mult))
                return;

            __result = (int)Math.Floor(__result * mult);
        }
    }
}
