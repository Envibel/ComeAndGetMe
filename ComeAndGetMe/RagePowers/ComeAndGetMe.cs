using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.Configurators.UnitLogic.ActivatableAbilities;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Conditions.Builder;
using BlueprintCore.Conditions.Builder.ContextEx;
using BlueprintCore.Utils.Types;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;

namespace ComeAndGetMe.RagePowers
{
    public class CAGE
    {
        private static readonly string RagePowerName = "ComeAndGetMeRagePower";
        private static readonly string RagePowerGuid = "63AEABA5-1974-4A97-9C9D-BCAD34C7B1D5";
        private static readonly string SwitchBuffName = "ComeAndGetMeSwitchBuff";
        private static readonly string SwitchBuffGuid = "1B1554BD-F3C2-41F9-8B94-0487D45EF9F1";
        private static readonly string EffectBuffName = "ComeAndGetMeEffectBuff";
        private static readonly string EffectBuffGuid = "2ACA6CA0-D80C-471C-A9CA-74694BF0003B";
        private static readonly string AbilityName = "ComeAndGetMeAbility";
        private static readonly string AbilityGuid = "4BAA4A2A-F2CE-401C-B501-A67524838CAB";
        private static readonly string DisplayName = "ComeAndGetMe.Name";
        private static readonly string Description = "ComeAndGetMe.Description";
        private static readonly string Icon = "assets/icons/cage.png";
        private static readonly int ComeAndGetMeACPenalty = -4;
        private static readonly int ComeAndGetMeDamageBonus = 4;

        public static void Configure()
        {
            // this buff provides the mechanical benefits of Come and Get Me!
            BuffConfigurator.New(EffectBuffName, EffectBuffGuid)
                .SetDisplayName("ComeAndGetMe.Name")
                .SetDescription("ComeAndGetMe.Description")
                .AddComponent<ComeAndGetMeTrigger>()
                .SetFlags(BlueprintBuff.Flags.HiddenInUi)
                .Configure();

            // If the player actives rage on a character with this buff then the character is given a hidden buff with
            // the actual mechanical effts of Come and Get Me!
            var switchbuff =
                BuffConfigurator.New(SwitchBuffName, SwitchBuffGuid)
                    .SetDisplayName("ComeAndGetMe.Name")
                    .SetDescription("ComeAndGetMe.Description")
                    .SetIcon(Icon)
                .Configure();

            // The ability that appears on the action bar and gives the character the SwitchBuffName
            var ability =
                ActivatableAbilityConfigurator.New(AbilityName, AbilityGuid)
                    .SetDisplayName(DisplayName)
                    .SetDescription(Description)
                    .SetIcon(Icon)
                    .SetBuff(switchbuff)
                    .Configure();

            FeatureConfigurator.New(RagePowerName, RagePowerGuid, FeatureGroup.RagePower)
                .AddPrerequisiteClassLevel(CharacterClassRefs.BarbarianClass.ToString(), 12)
                // A dummy pre req since the barbarian level pre req is not displayed without another pre req
                .AddPrerequisiteStatValue(StatType.Strength, 1)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .SetIcon(Icon)
                .AddFacts(new() { ability })
                .Configure();

            // Allow regular rage to proc Come and Get Me!
            BuffConfigurator.For(BuffRefs.StandartRageBuff)
                .AddFactContextActions(
                    activated:
                        ActionsBuilder.New()
                            .Conditional(
                                ConditionsBuilder.New().HasBuff(SwitchBuffName),
                                ifTrue: ActionsBuilder.New().ApplyBuffPermanent(EffectBuffName, isNotDispelable: true)))
                .Configure();

            //Allow focused rage to proc Come and Get Me!
            BuffConfigurator.For(BuffRefs.StandartFocusedRageBuff)
                .AddFactContextActions(
                    activated:
                        ActionsBuilder.New()
                            .Conditional(
                                ConditionsBuilder.New().HasFact(SwitchBuffName),
                                ifTrue: ActionsBuilder.New().ApplyBuffPermanent(EffectBuffName, isNotDispelable: true)))
                .Configure();

            // Allow bloodrage to proc Come and Get Me!
            BuffConfigurator.For(BuffRefs.BloodragerStandartRageBuff)
                .AddFactContextActions(
                    activated:
                        ActionsBuilder.New()
                        .Conditional(
                            ConditionsBuilder.New().HasFact(SwitchBuffName),
                            ifTrue: ActionsBuilder.New().ApplyBuffPermanent(EffectBuffName, isNotDispelable: true)))
                .Configure();
        }

        [TypeId("3D36DAB2-0A0D-473A-84E9-1C71243818CF")]
        private class ComeAndGetMeTrigger : UnitFactComponentDelegate, ITargetRulebookHandler<RuleDealDamage>, ITargetRulebookHandler<RuleAttackWithWeapon>, ITargetRulebookHandler<RuleCalculateAC>
        {
            public void OnEventAboutToTrigger(RuleCalculateAC evt)
            {

                evt.AddModifier(ComeAndGetMeACPenalty, this.Fact);
            }

            public void OnEventDidTrigger(RuleCalculateAC evt)
            {
            }


            public void OnEventAboutToTrigger(RuleDealDamage evt)
            {
                if (evt.DamageBundle.Count() > 0 && evt.Reason.Rule is RuleAttackWithWeapon)
                {
                    evt.DamageBundle.ElementAt(0).AddModifier(ComeAndGetMeDamageBonus, this.Fact);
                }

            }
            public void OnEventDidTrigger(RuleDealDamage evt)
            {
            }

            public void OnEventAboutToTrigger(RuleAttackWithWeapon evt)
            {

            }

            public void OnEventDidTrigger(RuleAttackWithWeapon evt)
            {
                if (this.Owner.Body.PrimaryHand.MaybeWeapon != null && this.Owner.Body.PrimaryHand.MaybeWeapon.Blueprint.IsMelee && evt.Weapon.Blueprint.IsMelee && this.Owner.CombatState.IsEngage(evt.Initiator))
                {
                    Game.Instance.CombatEngagementController.ForceAttackOfOpportunity(this.Owner, evt.Initiator);
                }
            }
        }

    }
}
