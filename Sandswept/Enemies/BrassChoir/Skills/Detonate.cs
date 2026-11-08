using System;
using System.Diagnostics.Contracts;
using Sandswept.Survivors;

namespace Sandswept.Enemies.BrassChoir.States {
    public class Detonate : BaseSkillState {
        public override void OnEnter()
        {
            base.OnEnter();

            for (int i = 0; i < BrassChoir.BellShotController.instancesList.Count; i++) {
                BrassChoir.BellShotController.instancesList[i].Explode();
            }

            outer.SetNextStateToMain();
        }
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death;
        }
    }

    public class DetonateSkill : SkillBase<DetonateSkill> {
        public override string Name => "";

        public override string Description => "";

        public override Type ActivationStateType => typeof(Detonate);

        public override string ActivationMachineName => "Weapon";

        public override float Cooldown => 1f;

        public override Sprite Icon => null;
        public override bool BeginCooldownOnSkillEnd => true;
        public override bool CanceledFromSprinting => false;
        public override bool FullRestockOnAssign => false;
    }
}