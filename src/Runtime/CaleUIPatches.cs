using System;
using System.Linq;
using System.Reflection;
using Harmony;
using UnityEngine;
using UnityEngine.UI;

namespace CaleHenituseAbnormality
{
    internal static class CaleUIPatches
    {
        static CaleUIPatches()
        {
            try
            {
                var workerUnitType = AccessTools.TypeByName("WorkerUnit");
                if (workerUnitType == null)
                    return;

                string[] candidates = new[] { "SetInfo", "Initialize", "Init", "Refresh", "UpdateUI", "SetWorker" };
                MethodInfo found = null;

                foreach (var name in candidates)
                {
                    var m = workerUnitType.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (m == null)
                        continue;

                    var pars = m.GetParameters();
                    if (pars.Any(p => p.ParameterType.Name == "WorkerModel"))
                    {
                        found = m;
                        break;
                    }
                }

                if (found == null)
                    return;

                var harmony = HarmonyInstance.Create("com.cale.ui.patch");
                var postfix = typeof(CaleUIPatches).GetMethod(nameof(WorkerPostfix), BindingFlags.Static | BindingFlags.NonPublic);
                harmony.Patch(found, null, new HarmonyMethod(postfix));
            }
            catch
            {
            }
        }

        // Postfix signature: receives instance
        private static void WorkerPostfix(object __instance)
        {
            try
            {
                if (__instance == null)
                    return;

                var wu = __instance as UnityEngine.Object;
                if (wu == null)
                    return;

                // Try to get workerModel field/property
                WorkerModel wm = null;
                try
                {
                    var t = __instance.GetType();
                    var f = t.GetField("workerModel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (f != null)
                        wm = f.GetValue(__instance) as WorkerModel;
                    else
                    {
                        var p = t.GetProperty("workerModel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (p != null && p.CanRead)
                            wm = p.GetValue(__instance, null) as WorkerModel;
                    }
                }
                catch { }

                if (wm == null)
                    return;

                string id = wm.instanceId.ToString();
                if (!CaleTeamManager.IsPromoted(id))
                    return;

                // Check if label already exists
                var go = (wu as GameObject) ?? (wu as Component)?.gameObject;
                if (go == null)
                    return;

                if (go.transform.Find("CaleCommanderLabel") != null)
                    return;

                // Create UI Text label
                var canvas = go.GetComponentInChildren<Canvas>();
                GameObject parent = canvas != null ? canvas.gameObject : go;

                GameObject label = new GameObject("CaleCommanderLabel");
                label.transform.SetParent(parent.transform, false);

                var txt = label.AddComponent<Text>();
                txt.text = "Главнокомандующий";
                txt.color = new Color(0.6f, 0f, 0f, 1f);
                txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                txt.raycastTarget = false;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.fontSize = 14;

                // Set RectTransform anchoring near top-right of parent
                var rt = label.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-10f, -10f);
                rt.sizeDelta = new Vector2(160f, 22f);
            }
            catch
            {
            }
        }
    }
}
