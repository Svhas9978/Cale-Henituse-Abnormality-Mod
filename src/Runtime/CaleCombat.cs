using System;
using System.Collections.Generic;
using System.Reflection;
using GeburahBoss;
using UnityEngine;

namespace CaleHenituseAbnormality
{
    internal static class CaleCombat
    {

        public static void ApplyCaleWalkSpeed(CreatureModel model)
        {
            if (model == null)
                return;

            try
            {
                model.AddUnitBuf(new MovementBuf(0.35f));
            }
            catch
            {
            }
        }

        public static void ApplyCaleStopSpeed(CreatureModel model)
        {
            if (model == null)
                return;

            try
            {
                model.AddUnitBuf(new MovementBuf(0.01f));
            }
            catch
            {
            }
        }

        public static void ApplyComfortAura(CaleHenituseAbnormality source, float radius, float amount)
        {
            if (source == null || source.Unit == null)
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

                if (Vector3.Distance(view.transform.position, source.Unit.transform.position) > radius)
                    continue;

                worker.RecoverHP(amount * Time.fixedDeltaTime);
                worker.RecoverMental(amount * Time.fixedDeltaTime);
            }
        }

        public static void ApplyMentalShock(AgentModel agent, float amount)
        {
            if (agent == null)
                return;

            try
            {
                agent.TakeDamage(new DamageInfo(RwbpType.W, amount));
            }
            catch
            {
                // If a future Assembly-CSharp changes the damage path,
                // the suppression still fails and the state transition remains.
            }
        }

        public static void ApplyDepartmentShockAndGuilt(
            CaleHenituseAbnormality cale,
            float whiteDamage,
            int workSpeedPenalty)
        {
            if (cale == null || cale.model == null)
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

                // 50 WHITE shock to every survivor in the department.
                try
                {
                    worker.TakeDamage(new DamageInfo(RwbpType.W, whiteDamage));
                }
                catch
                {
                }

                CaleGuiltRegistry.Apply(worker, workSpeedPenalty, 1f);
            }
        }

        private static OrdealCreatureModel[] GetOrdealCreatures()
        {
            try
            {
                var manager = OrdealManager.instance;
                if (manager == null)
                    return Array.Empty<OrdealCreatureModel>();

                var list = manager.GetOrdealCreatureList();
                return list ?? Array.Empty<OrdealCreatureModel>();
            }
            catch
            {
                return Array.Empty<OrdealCreatureModel>();
            }
        }

        public static void ClearOrdealCreaturesInDepartment(CaleHenituseAbnormality cale)
        {
            if (cale == null || cale.model == null)
                return;

            var ordeals = GetOrdealCreatures();

            for (int i = 0; i < ordeals.Length; i++)
            {
                var ordeal = ordeals[i];
                if (ordeal == null || ordeal.hp <= 0)
                    continue;

                if (!string.Equals(ordeal.sefiraNum, cale.model.sefiraNum, StringComparison.OrdinalIgnoreCase))
                    continue;

                bool removed = TryInvokeKill(ordeal);

                if (!removed)
                {
                    TryHugeDamage(ordeal, RwbpType.R);
                    TryHugeDamage(ordeal, RwbpType.W);
                    TryHugeDamage(ordeal, RwbpType.B);
                    TryHugeDamage(ordeal, RwbpType.P);
                }
            }
        }

        private static bool TryInvokeKill(UnitModel target)
        {
            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            string[] candidates =
            {
                "Die", "Suppress", "OnSuppressed", "EscapeWithoutIsolateRoom"
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                MethodInfo method = target.GetType().GetMethod(candidates[i], flags, null, Type.EmptyTypes, null);
                if (method == null)
                    continue;

                try
                {
                    method.Invoke(target, null);
                    return true;
                }
                catch
                {
                }
            }

            return false;
        }

        private static void TryHugeDamage(UnitModel target, RwbpType type)
        {
            try
            {
                target.TakeDamage(new DamageInfo(type, 9999999f));
            }
            catch
            {
            }
        }

        public static void PlaySilverShieldEffect(CaleHenituseAbnormality cale)
        {
            if (cale == null || cale.model == null)
                return;

            // Uses BaseMod-compatible visual effect hooks when available.
            try
            {
                cale.MakeEffectGlobalPosNonTrans(
                    "CaleSilverShield",
                    cale.model.GetCurrentViewPosition());
            }
            catch
            {
                // The gameplay effect is authoritative; missing optional VFX should not crash the day.
            }
        }

        public static bool IsInSameRoom(AgentModel actor, UnitModel source)
        {
            if (actor == null || source == null)
                return false;

            try
            {
                return Vector3.Distance(
                           actor.GetCurrentViewPosition(),
                           source.GetCurrentViewPosition()) <= 2.25f;
            }
            catch
            {
                return false;
            }
        }

        public static List<UnitModel> FindNearbyUnits(UnitModel source, float radius)
        {
            var result = new List<UnitModel>();
            if (source == null)
                return result;

            var workers = UnityEngine.Object.FindObjectsOfType<WorkerUnit>();
            for (int i = 0; i < workers.Length; i++)
            {
                WorkerUnit view = workers[i];
                if (view == null || view.workerModel == null)
                    continue;

                UnitModel target = view.workerModel;

                if (target == source || target.hp <= 0)
                    continue;

                if (Vector3.Distance(
                        view.transform.position,
                        source.GetCurrentViewPosition()) <= radius)
                    result.Add(target);
            }

            return result;
        }
    }
}
