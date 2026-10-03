using System;
using System.Collections.Generic;
using Harmony;
using UnityEngine;

namespace CaleHenituseAbnormality
{
    internal static class CaleDeathTracker
    {
        private static readonly Dictionary<string, int> AgentsDead = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> ClerksDead = new Dictionary<string, int>();

        public static void ResetDepartmentCounters()
        {
            AgentsDead.Clear();
            ClerksDead.Clear();
        }

        public static void RegisterAgentDeath(AgentModel agent)
        {
            if (agent == null)
                return;

            string dept = GetDepartment(agent);
            Increment(AgentsDead, dept);
            TryAllCaleShields(dept);
        }

        public static void RegisterClerkDeath(WorkerModel worker)
        {
            if (worker == null)
                return;

            string dept = GetDepartment(worker);
            Increment(ClerksDead, dept);
            TryAllCaleShields(dept);
        }

        public static void TryTriggerEligibleShield(CaleHenituseAbnormality cale)
        {
            if (cale == null || cale.model == null || cale.ShieldUsedToday)
                return;

            string dept = cale.model.sefiraNum;
            // Require at least one linked worker in the department before shield can trigger.
            if (CaleTeamManager.GetLinkedCount(dept) <= 0)
                return;

            if (!IsOrdealActiveInDepartment(dept))
                return;

            int agents = Get(AgentsDead, dept);
            int clerks = Get(ClerksDead, dept);

            if (agents >= 1 || clerks >= 3)
                cale.TriggerSilverShield();
        }

        private static void TryAllCaleShields(string dept)
        {
            if (!IsOrdealActiveInDepartment(dept))
                return;

            foreach (var cale in CaleHenituseAbnormality.Instances)
            {
                if (cale == null || cale.model == null)
                    continue;

                if (!string.Equals(cale.model.sefiraNum, dept, StringComparison.OrdinalIgnoreCase))
                    continue;

                TryTriggerEligibleShield(cale);
            }
        }

        private static bool IsOrdealActiveInDepartment(string dept)
        {
            if (string.IsNullOrEmpty(dept))
                return false;

            var manager = OrdealManager.instance;
            var ordeals = manager != null ? manager.GetOrdealCreatureList() : Array.Empty<OrdealCreatureModel>();
            for (int i = 0; i < ordeals.Length; i++)
            {
                var ordeal = ordeals[i];
                if (ordeal == null || ordeal.hp <= 0)
                    continue;

                if (string.Equals(ordeal.sefiraNum, dept, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string GetDepartment(WorkerModel worker)
        {
            if (worker == null)
                return string.Empty;

            return worker.currentSefira ?? string.Empty;
        }

        private static void Increment(Dictionary<string, int> dict, string key)
        {
            if (!dict.ContainsKey(key))
                dict[key] = 0;

            dict[key]++;
        }

        private static int Get(Dictionary<string, int> dict, string key)
        {
            int value;
            return dict.TryGetValue(key, out value) ? value : 0;
        }
    }
    [HarmonyPatch(typeof(WorkerModel), "OnDie")]
    internal static class CaleWorkerDeathPatch
    {
        [HarmonyPostfix]
        private static void Postfix(WorkerModel __instance)
        {
            AgentModel agent = __instance as AgentModel;
            if (agent != null)
                CaleDeathTracker.RegisterAgentDeath(agent);
            else
                CaleDeathTracker.RegisterClerkDeath(__instance);
        }
    }
}
