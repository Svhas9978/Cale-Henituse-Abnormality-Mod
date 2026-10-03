using System;
using System.Collections.Generic;
using UnityEngine;

namespace CaleHenituseAbnormality
{
    internal static class CaleTeamManager
    {
        // dept -> set of worker instance ids
        private static readonly Dictionary<string, HashSet<string>> linkedByDept = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        // dept -> workerId -> attachment count
        private static readonly Dictionary<string, Dictionary<string, int>> attachmentCounts = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> promotedWorkers = new HashSet<string>();

        private static int lastObservedDay = -1;

        public static void ResetDailyIfNeeded(Func<int> readDay)
        {
            try
            {
                int day = readDay?.Invoke() ?? -1;
                if (day < 0)
                    return;

                if (day != lastObservedDay)
                {
                    // new day -> clear per-day promotion markers
                    promotedWorkers.Clear();
                    lastObservedDay = day;
                }
            }
            catch
            {
            }
        }

        public static void RegisterLink(string dept, string workerId)
        {
            if (string.IsNullOrEmpty(dept) || string.IsNullOrEmpty(workerId))
                return;

            if (!linkedByDept.TryGetValue(dept, out var set))
            {
                set = new HashSet<string>();
                linkedByDept[dept] = set;
            }

            set.Add(workerId);
        }

        public static bool IsWorkerLinked(string dept, string workerId)
        {
            if (string.IsNullOrEmpty(dept) || string.IsNullOrEmpty(workerId))
                return false;

            return linkedByDept.TryGetValue(dept, out var set) && set.Contains(workerId);
        }

        public static int GetLinkedCount(string dept)
        {
            if (string.IsNullOrEmpty(dept))
                return 0;

            return linkedByDept.TryGetValue(dept, out var set) ? set.Count : 0;
        }

        public static bool IsFullTeam(string dept, int expectedCount)
        {
            // If expectedCount is provided (>0) use it; otherwise compare against current number
            // of active workers in the department.
            if (expectedCount > 0 && expectedCount < Int32.MaxValue)
                return GetLinkedCount(dept) >= expectedCount;

            try
            {
                int total = 0;
                var workers = UnityEngine.Object.FindObjectsOfType<WorkerUnit>();
                for (int i = 0; i < workers.Length; i++)
                {
                    var view = workers[i];
                    if (view == null || view.workerModel == null)
                        continue;

                    var wm = view.workerModel;
                    if (wm.hp <= 0)
                        continue;

                    if (string.Equals(wm.currentSefira, dept, StringComparison.OrdinalIgnoreCase))
                        total++;
                }

                return GetLinkedCount(dept) >= total && total > 0;
            }
            catch
            {
                return false;
            }
        }

        public static void IncrementAttachment(string dept, string workerId, Action promoteAction = null)
        {
            if (string.IsNullOrEmpty(dept) || string.IsNullOrEmpty(workerId))
                return;

            if (!attachmentCounts.TryGetValue(dept, out var dict))
            {
                dict = new Dictionary<string, int>();
                attachmentCounts[dept] = dict;
            }

            if (!dict.ContainsKey(workerId))
                dict[workerId] = 0;

            dict[workerId]++;

            if (dict[workerId] >= 10 && !promotedWorkers.Contains(workerId))
            {
                promotedWorkers.Add(workerId);
                try { promoteAction?.Invoke(); } catch { }
            }
        }

        public static bool IsPromoted(string workerId)
        {
            if (string.IsNullOrEmpty(workerId))
                return false;

            return promotedWorkers.Contains(workerId);
        }
    }
}
