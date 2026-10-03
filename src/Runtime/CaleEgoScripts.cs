using System;
using System.Collections.Generic;
using GeburahBoss;
using Harmony;
using UnityEngine;

namespace CaleHenituseAbnormality
{
    public sealed class CaleTreeSwordScript : EquipmentScriptBase
    {
        private int hitCounter;

        public override void OnStageStart()
        {
            base.OnStageStart();
            hitCounter = 0;
        }

        public override EquipmentScriptBase.WeaponDamageInfo OnAttackStart(UnitModel actor, UnitModel target)
        {
            hitCounter++;

            bool fifth = hitCounter % 5 == 0;

            if (fifth && target != null)
                target.AddUnitBuf(new MovementBuf(0.5f, 3f));

            // The vanilla/base attack damage is kept in the equipment data.
            // The special hit is added in OnGiveDamageAfter.
            return base.OnAttackStart(actor, target);
        }

        public override void OnGiveDamageAfter(UnitModel actor, UnitModel target, DamageInfo dmg)
        {
            base.OnGiveDamageAfter(actor, target, dmg);

            if (actor == null || target == null)
                return;

            if (hitCounter % 5 != 0)
                return;

            bool hasGift = actor.HasEquipment(CaleHenituseAbnormality.GiftId);

            if (hasGift && UnityEngine.Random.value <= 0.10f)
            {
                try
                {
                    target.TakeDamage(new DamageInfo(RwbpType.P, 999999f));
                }
                catch
                {
                }
            }
        }
    }

    public sealed class CaleCommanderArmorScript : EquipmentScriptBase
    {
        public override void OnEquip(UnitModel actor)
        {
            base.OnEquip(actor);
            if (actor == null)
                return;
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
        }
    }

    public sealed class CaleAncientPermissionGiftScript : EquipmentScriptBase
    {
        private float invincibleUntil;

        public override void OnEquip(UnitModel actor)
        {
            base.OnEquip(actor);

            if (actor == null)
                return;

            WorkerModel worker = actor as WorkerModel;
            if (worker == null)
                return;

            // Register link: only workers in the same department as a Cale instance are valid.
            string dept = worker.currentSefira ?? string.Empty;
            if (!string.IsNullOrEmpty(dept))
            {
                try
                {
                    string id = worker.instanceId.ToString();
                    CaleTeamManager.RegisterLink(dept, id);
                }
                catch
                {
                }
            }
        }

        public override void OnStageStart()
        {
            base.OnStageStart();
            invincibleUntil = 0f;
        }

        public override void OnGiveDamageAfter(UnitModel actor, UnitModel target, DamageInfo dmg)
        {
            base.OnGiveDamageAfter(actor, target, dmg);

            if (actor == null || target == null)
                return;

            if (!actor.HasEquipment(CaleHenituseAbnormality.WeaponId))
                return;

            if (UnityEngine.Random.value > 0.10f)
                return;

            var nearby = CaleCombat.FindNearbyUnits(actor, 2.4f);

            for (int i = 0; i < nearby.Count; i++)
            {
                UnitModel unit = nearby[i];

                try
                {
                    unit.TakeDamage(new DamageInfo(RwbpType.R, 40f));
                    unit.TakeDamage(new DamageInfo(RwbpType.P, 40f));
                }
                catch
                {
                }
            }

            // Momentary "Unbreakable Shield". Kept local so it cannot accidentally persist.
            WorkerModel worker = actor as WorkerModel;
            if (worker != null)
            {
                worker.SetInvincible(true);
                invincibleUntil = Time.time + 0.20f;
            }
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            if (invincibleUntil <= 0f)
                return;

            if (Time.time >= invincibleUntil)
            {
                if (model != null && model.owner is WorkerModel worker)
                    worker.SetInvincible(false);

                invincibleUntil = 0f;
            }
        }
    }

    // The armor aura is global because the vanilla equipment script only receives
    // the owner, while the requested effect covers all allies in the same room.
    [HarmonyPatch(typeof(UnitModel), "GetMovementBuf")]
    internal static class CaleArmorMovementAuraPatch
    {
        [HarmonyPostfix]
        private static void Postfix(UnitModel __instance, ref float __result)
        {
            WorkerModel worker = __instance as WorkerModel;
            if (worker == null || !HasCaleArmor(worker))
                return;

            if (!IsNearArmoredAlly(worker))
                return;

            __result += 30f;
        }

        private static bool HasCaleArmor(WorkerModel worker)
        {
            return worker != null && worker.HasEquipment(CaleHenituseAbnormality.ArmorId);
        }

        private static bool IsNearArmoredAlly(WorkerModel target)
        {
            var workers = UnityEngine.Object.FindObjectsOfType<WorkerUnit>();

            for (int i = 0; i < workers.Length; i++)
            {
                var view = workers[i];
                if (view == null || view.workerModel == null)
                    continue;

                WorkerModel source = view.workerModel;

                if (source == target || source.hp <= 0)
                    continue;

                if (!source.HasEquipment(CaleHenituseAbnormality.ArmorId))
                    continue;

                if (!string.Equals(source.currentSefira, target.currentSefira, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (Vector3.Distance(view.transform.position, target.GetCurrentViewPosition()) <= 3.0f)
                    return true;
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(UnitModel), "GetAttackSpeedBuf")]
    internal static class CaleArmorAttackAuraPatch
    {
        [HarmonyPostfix]
        private static void Postfix(UnitModel __instance, ref float __result)
        {
            WorkerModel worker = __instance as WorkerModel;
            if (worker == null)
                return;

            var workers = UnityEngine.Object.FindObjectsOfType<WorkerUnit>();

            for (int i = 0; i < workers.Length; i++)
            {
                var view = workers[i];
                if (view == null || view.workerModel == null)
                    continue;

                WorkerModel source = view.workerModel;

                if (source == worker || source.hp <= 0)
                    continue;

                if (!source.HasEquipment(CaleHenituseAbnormality.ArmorId))
                    continue;

                if (!string.Equals(source.currentSefira, worker.currentSefira, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (Vector3.Distance(view.transform.position, worker.GetCurrentViewPosition()) <= 3.0f)
                {
                    __result += 30f;
                    return;
                }
            }
        }
    }
}
