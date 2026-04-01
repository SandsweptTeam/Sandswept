using System;
using System.Linq;
using JetBrains.Annotations;
using RoR2.CharacterAI;
using Sandswept.Survivors;

namespace Sandswept.Enemies.Ivy {
    public class BeginTargetSearch : BaseSkillState {
        public float maxSearchDistance = 80f;
        public override void OnEnter()
        {
            base.OnEnter();

            if (NetworkServer.active) {
                SphereSearch search = new();
                search.origin = base.transform.position;
                search.radius = maxSearchDistance;
                search.mask = LayerIndex.entityPrecise.mask;
                search.RefreshCandidates();
                search.FilterCandidatesByDistinctHurtBoxEntities();
                TeamMask filter = new();
                filter.AddTeam(GetTeam());
                search.FilterCandidatesByHurtBoxTeam(filter);
                HurtBox[] results = search.GetHurtBoxes();
                results = results
                .OrderByDescending(x => x.healthComponent.fullCombinedHealth)
                .Where(x => !x.healthComponent.body.isBoss && x.healthComponent.body != base.characterBody && FilterRangedEnemy(x.healthComponent.body))
                .ToArray();
                

                if (results.Length > 0) {
                    base.characterBody.master.aiComponents[0].customTarget.gameObject = results[0].healthComponent.gameObject;
                    skillLocator.secondary.DeductStock(1);
                }
            }

            outer.SetNextStateToMain();
        }

        public static List<LazyIndex> Blacklist = new() {
            new("BisonBody"), new("HalcyoniteBody"), new("DeltaConstructBody") /* hitler */, new("CannonJellyBody"),
            new("LarvaBody"), new("IvyBody") /* source engine */, new("WorkerUnitBody"), new("MinePodBody"), new("GolemBody"), // golem laser doesnt like aspd changes
            new("ParentBody"), new("GupBody") /* fat fuck */, new("IvyHeadBody")
        };

        public bool FilterRangedEnemy(CharacterBody body) {
            CharacterMaster master = body.master;
            if (!master) {
                return false;
            }

            foreach (LazyIndex index in Blacklist) {
                if (body.bodyIndex == index) {
                    return false;
                }
            }

            AISkillDriver[] skillDrivers = master.GetComponents<AISkillDriver>();
            for (int i = 0; i < skillDrivers.Length; i++) {
                AISkillDriver driver = skillDrivers[i];

                if (driver.maxDistance > 15f && driver.skillSlot != SkillSlot.None) {
                    GenericSkill slot = driver.skillSlot switch {
                        SkillSlot.Primary => body.skillLocator.primary,
                        SkillSlot.Secondary => body.skillLocator.secondary,
                        SkillSlot.Utility => body.skillLocator.utility,
                        SkillSlot.Special => body.skillLocator.special,
                        _ => null
                    };

                    if (slot && slot.isCombatSkill) {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    public class BeginSearchSkill : SkillBase<BeginSearchSkill>
    {
        public override string Name => "";

        public override string Description => "";

        public override Type ActivationStateType => typeof(BeginTargetSearch);

        public override string ActivationMachineName => "Weapon";

        public override float Cooldown => 1f;

        public override Sprite Icon => null;
        public override bool BeginCooldownOnSkillEnd => true;
        public override bool CanceledFromSprinting => false;
        public override void SetupSkillDef()
        {
            base.SetupSkillDef();
            skillDef.stockToConsume = 0;
        }
    }
}