using System;
using System.Reflection;
using Harmony;
using UnityEngine;

namespace CaleHenituseAbnormality
{
    [HarmonyPatch]
    internal static class CaleTeamPatches
    {
        // Try to patch WorkerModel.FinishWork or OnFinishWork to catch when a worker finishes Attachment.
        public static MethodBase TargetMethod()
        {
            Type workerType = AccessTools.TypeByName("WorkerModel");
            if (workerType == null)
                return null;

            // Prefer method named "FinishWork" with one parameter.
            MethodInfo m = workerType.GetMethod("FinishWork", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m != null)
                return m;

            m = workerType.GetMethod("OnFinishWork", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return m;
        }

        // Postfix will receive the instance and the skill argument when available.
        public static void Postfix(object __instance, object skill)
        {
            try
            {
                if (__instance == null)
                    return;

                WorkerModel worker = __instance as WorkerModel;
                if (worker == null)
                    return;

                // Determine the skill rwbp type via reflection-safe helper.
                RwbpType type = default(RwbpType);
                try
                {
                    UseSkill us = skill as UseSkill;
                    if (us != null && us.skillTypeInfo != null)
                        type = us.skillTypeInfo.rwbpType;
                }
                catch { }

                if (type != RwbpType.B)
                    return;

                string dept = worker.currentSefira ?? string.Empty;
                string id = worker.instanceId.ToString();

                CaleTeamManager.IncrementAttachment(dept, id, () => PromoteWorkerToCommander(worker));
            }
            catch
            {
            }
        }

        private static void PromoteWorkerToCommander(WorkerModel worker)
        {
            try
            {
                if (worker == null)
                    return;

                // Change hair color if currently white
                CalePromotion.TrySetCommanderHair(worker);
            }
            catch
            {
            }
        }
    }

    internal static class CalePromotion
    {
        private static readonly Color BloodRed = new Color(0.6f, 0.0f, 0.0f, 1f);

        public static void TrySetCommanderHair(WorkerModel worker)
        {
            if (worker == null)
                return;

            try
            {
                // Try model-level fields/properties first
                if (TrySetHairOnObject(worker))
                    return;

                // Try view-level (WorkerUnit)
                WorkerUnit[] units = UnityEngine.Object.FindObjectsOfType<WorkerUnit>();
                for (int i = 0; i < units.Length; i++)
                {
                    var u = units[i];
                    if (u == null)
                        continue;

                    try
                    {
                        var vm = u.workerModel;
                        if (vm == null)
                            continue;

                        if (vm.instanceId != worker.instanceId)
                            continue;

                        if (TrySetHairOnObject(u))
                            return;

                        // Try common SpriteRenderer fields on the view that include 'hair' in name
                        var vt = u.GetType();
                        var fields = vt.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        for (int f = 0; f < fields.Length; f++)
                        {
                            var fi = fields[f];
                            if (!fi.Name.ToLowerInvariant().Contains("hair"))
                                continue;

                            object val = fi.GetValue(u);
                            if (val is SpriteRenderer sr)
                            {
                                if (IsColorWhite(sr.color))
                                    sr.color = BloodRed;
                                return;
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static bool TrySetHairOnObject(object obj)
        {
            if (obj == null)
                return false;

            Type t = obj.GetType();

            // Check fields
            var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < fields.Length; i++)
            {
                var f = fields[i];
                if (!f.Name.ToLowerInvariant().Contains("hair"))
                    continue;

                object val = null;
                try { val = f.GetValue(obj); } catch { }
                if (val == null)
                    continue;

                if (val is Color c)
                {
                    try { f.SetValue(obj, BloodRed); } catch { }
                    return true;
                }
                else if (val is SpriteRenderer sr)
                {
                    try { sr.color = BloodRed; } catch { }
                    return true;
                }
                else if (val is string s)
                {
                    try { f.SetValue(obj, "#8B0000"); } catch { }
                    return true;
                }
            }

            // Check properties
            var props = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < props.Length; i++)
            {
                var p = props[i];
                if (!p.CanRead || !p.Name.ToLowerInvariant().Contains("hair"))
                    continue;

                object val = null;
                try { val = p.GetValue(obj, null); } catch { }
                if (val == null)
                    continue;

                if (val is Color c)
                {
                    if (p.CanWrite) { try { p.SetValue(obj, BloodRed, null); } catch { } }
                    return true;
                }
                else if (val is SpriteRenderer sr)
                {
                    try { sr.color = BloodRed; } catch { }
                    return true;
                }
                else if (val is string s)
                {
                    if (p.CanWrite) { try { p.SetValue(obj, "#8B0000", null); } catch { } }
                    return true;
                }
            }

            return false;
        }

        private static bool IsColorWhite(Color c)
        {
            return c.r >= 0.95f && c.g >= 0.95f && c.b >= 0.95f;
        }

        private static bool IsStringWhiteColor(string s)
        {
            if (string.IsNullOrEmpty(s))
                return false;

            s = s.Trim().ToLowerInvariant();
            if (s == "white" || s == "#ffffff" || s == "#fff")
                return true;

            return false;
        }
    }
}
