using System;
using System.Linq;
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
                .Where(x => !x.healthComponent.body.isBoss)
                .ToArray();
                

                if (results.Length > 0) {
                    base.characterBody.master.aiComponents[0].customTarget.gameObject = results[0].healthComponent.gameObject;
                    skillLocator.secondary.DeductStock(1);
                }
            }

            outer.SetNextStateToMain();
        }
    }

    public class BeginSearchSkill : SkillBase<BeginSearchSkill>
    {
        public override string Name => "";

        public override string Description => "";

        public override Type ActivationStateType => typeof(BeginTargetSearch);

        public override string ActivationMachineName => "Weapon";

        public override float Cooldown => 15f;

        public override Sprite Icon => null;
        public override bool BeginCooldownOnSkillEnd => true;
        public override bool CanceledFromSprinting => false;
        public override bool ManualTrackedCooldown => true;
    }
}