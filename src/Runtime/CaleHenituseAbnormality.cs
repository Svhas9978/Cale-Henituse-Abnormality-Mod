using System;
using System.Collections.Generic;
using System.Reflection;
using Harmony;
using UnityEngine;

namespace CaleHenituseAbnormality
{
    /// <summary>
    /// F-04-18 / Cale Henituse.
    ///
    /// API-facing pieces deliberately use CreatureBase's stable hooks:
    /// ParamInit, OnStageStart, OnStageEnd, OnWorkAllocated,
    /// ForcelyFail, ReducedQliphothCounter, AddedQliphothCounter,
    /// ResetQliphothCounter, SpecialEnergyTick, UniqueEscape,
    /// CanTakeDamage and OnSuppressed.
    /// </summary>
    public sealed class CaleHenituseAbnormality : CreatureBase
    {
        public const long AbnormalityId = 94180418;
        public const int ArmorId = 94180420;
        public const int WeaponId = 94180419;
        public const int GiftId = 94180421;

        private const float AuraRadius = 3.5f;
        private const float AuraHealPerTick = 2.5f;

        private bool wandering;
        private bool sleeping;
        private bool shieldUsedToday;

        internal static readonly HashSet<CaleHenituseAbnormality> Instances =
            new HashSet<CaleHenituseAbnormality>();

        public bool IsWandering { get { return wandering; } }
        public bool IsSleeping { get { return sleeping; } }
        public bool ShieldUsedToday { get { return shieldUsedToday; } }

        public override void ParamInit()
        {
            base.ParamInit();
            Instances.Add(this);
            wandering = false;
            sleeping = false;
            shieldUsedToday = false;
            CaleStoryState.InitializeRun();
        }

        public override void OnStageStart()
        {
            base.OnStageStart();

            Instances.Add(this);

            // The shield sleep lasts until the next day.
            sleeping = false;
            wandering = false;
            shieldUsedToday = false;

            CaleDeathTracker.ResetDepartmentCounters();

            try
            {
                if (model != null)
                    model.ResetQliphothCounter();
            }
            catch (Exception e)
            {
                CaleLog.Exception("Failed to reset Cale qliphoth.", e);
            }

            CaleStoryState.ObserveDayBoundary();
            // Reset per-day counters in the team manager (promotions, etc.).
            CaleTeamManager.ResetDailyIfNeeded(CaleStoryState.GetCurrentDay);
        }

        public override void OnStageEnd()
        {
            base.OnStageEnd();
            CaleStoryState.ObserveStageEnd();
        }

        public override void OnStageRelease()
        {
            base.OnStageRelease();
            Instances.Remove(this);
        }

        public override bool CanTakeDamage(UnitModel attacker, DamageInfo damageInfo)
        {
            if (wandering || sleeping)
                return false;

            return base.CanTakeDamage(attacker, damageInfo);
        }

        public override bool ForcelyFail(UseSkill skill)
        {
            if (IsSuppression(skill))
                return true;

            return base.ForcelyFail(skill);
        }

        public override bool ForcelySuccess(UseSkill skill)
        {
            return base.ForcelySuccess(skill);
        }

        public override void OnWorkAllocated(SkillTypeInfo skill, AgentModel agent)
        {
            base.OnWorkAllocated(skill, agent);

            if (agent == null || skill == null)
                return;

            var type = GetSkillRwbpType(skill);

            // Attachment is the deliberate "return to silence" work.
            if (wandering && type == RwbpType.B)
            {
                ReturnToContainment();
                return;
            }

            // Suppression is always rejected. Give a very large WHITE shock.
            if (type == RwbpType.P)
            {
                CaleCombat.ApplyMentalShock(agent, 999f);
                StartWandering();
                CaleStoryState.MarkManualSuppressionAttempt();
            }
        }

        public override void OnFinishWork(UseSkill skill)
        {
            base.OnFinishWork(skill);

            if (skill == null)
                return;

            var type = GetSkillRwbpType(skill);

            if (type == RwbpType.B && !wandering && !sleeping)
                CaleStoryState.RegisterAttachmentResult(this);

            if (type == RwbpType.R)
                CaleStoryState.RegisterAnyInstinctWork();
        }

        public override void ReducedQliphothCounter()
        {
            // Qliphoth still tracked, but it no longer causes wandering/escape.
            if (sleeping)
                return;

            base.ReducedQliphothCounter();

            // Do NOT start wandering or escape when counter reaches zero.
        }

        public override void AddedQliphothCounter()
        {
            if (sleeping)
                return;

            base.AddedQliphothCounter();
        }

        public override void ResetQliphothCounter()
        {
            if (sleeping)
                return;

            base.ResetQliphothCounter();
        }

        public override int GetQliphothCounterMax()
        {
            return 3;
        }

        public override float SpecialEnergyTick()
        {
            if (sleeping)
                return 0f;

            return base.SpecialEnergyTick();
        }

        public override void UniqueEscape()
        {
            // Deprecated: UniqueEscape no longer performs wandering/escape logic.
            // Keep as a no-op for compatibility.
        }

        public override void Escape()
        {
            // Disabled: Cale does not escape.
            return;
        }

        public override bool IsSuppressable()
        {
            return false;
        }

        public override bool IsSuppressableByRoom()
        {
            return false;
        }

        public override bool IsAutoSuppressable()
        {
            return false;
        }

        public override bool CanEnterRoom()
        {
            return true;
        }

        public override bool IsActivatedWorkDesc()
        {
            return true;
        }

        public void StartWandering()
        {
            // Wandering/escape removed; ensure state remains non-wandering.
            wandering = false;
        }

        private void ReturnToContainment()
        {
            // No-op: containment/return logic removed for wandering-less behavior.
            wandering = false;
        }

        internal void TriggerSilverShield()
        {
            if (shieldUsedToday || sleeping)
                return;

            // Require at least one linked worker in department before shield triggers.
            string dept = model != null ? model.sefiraNum : string.Empty;
            if (CaleTeamManager.GetLinkedCount(dept) <= 0)
                return;

            shieldUsedToday = true;
            wandering = false;
            sleeping = true;

            try
            {
                SetQliphothForState(0);

                if (model != null)
                {
                    // Do not perform EscapeWithoutIsolateRoom; just stop movement and clear ordeal creatures.
                    CaleCombat.ApplyCaleStopSpeed(model);
                    CaleCombat.ClearOrdealCreaturesInDepartment(this);
                }

                CaleCombat.PlaySilverShieldEffect(this);

                // Only apply the 50 WHITE damage & guilt when the team is fully formed.
                // Pass 0 to use dynamic detection against active workers in department.
                bool full = CaleTeamManager.IsFullTeam(dept, 0);
                if (full)
                {
                    CaleCombat.ApplyDepartmentShockAndGuilt(this, 50f, -15);
                }

                CaleStoryState.RegisterSilverShieldUse();
            }
            catch (Exception e)
            {
                CaleLog.Exception("Silver Shield trigger failed.", e);
            }
        }

        private void SetQliphothForState(int value)
        {
            if (model == null)
                return;

            var field = typeof(CreatureModel).GetField(
                "_qliphothCounter",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (field != null)
                field.SetValue(model, value);
        }

        private static RwbpType GetSkillRwbpType(SkillTypeInfo skill)
        {
            if (skill == null)
                return default(RwbpType);

            return skill.rwbpType;
        }

        private static RwbpType GetSkillRwbpType(UseSkill skill)
        {
            if (skill == null || skill.skillTypeInfo == null)
                return default(RwbpType);

            return skill.skillTypeInfo.rwbpType;
        }

        private static bool IsSuppression(UseSkill skill)
        {
            return skill != null && GetSkillRwbpType(skill) == RwbpType.P;
        }
    }
}
