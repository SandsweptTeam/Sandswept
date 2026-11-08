using System;
using System.Diagnostics.Contracts;
using Sandswept.Survivors;

namespace Sandswept.Enemies.BrassChoir.States {
    public class BellAura : BaseSkillState {
        public float duration = 10f;

        public override void OnEnter()
        {
            base.OnEnter();

            GetComponent<BuffWard>().Networkradius = 120f;

            if (base.isAuthority) {
                BrassChoir.ReplicateToSpires(base.characterBody, SkillSlot.Special);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= duration) {
                outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            base.OnExit();

            GetComponent<BuffWard>().Networkradius = 0f;
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death;
        }
    }

    public class BellAuraSkill : SkillBase<BellAuraSkill> {
        public override string Name => "";

        public override string Description => "";

        public override Type ActivationStateType => typeof(BellAura);

        public override string ActivationMachineName => "Weapon";

        public override float Cooldown => 15f;

        public override Sprite Icon => null;
        public override bool BeginCooldownOnSkillEnd => true;
        public override bool CanceledFromSprinting => false;
        public override bool FullRestockOnAssign => false;
    }
}