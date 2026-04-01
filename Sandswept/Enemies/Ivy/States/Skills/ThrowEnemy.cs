using System;
using System.Linq;
using Sandswept.Survivors;

namespace Sandswept.Enemies.Ivy {
    public class ThrowEnemy : BaseSkillState {
        public float duration = 0.8f;
        public AnimEventTracker anim;
        public VehicleSeat seat;
        public bool startedAnim = false;
        public IvyModelController controller;
        public override void OnEnter()
        {
            base.OnEnter();

            anim = new(GetModelAnimator());
            seat = GetComponent<VehicleSeat>();

            PlayAnimation("Override, Head", "Idle");

            controller = GetModelTransform().GetComponent<IvyModelController>();
            controller.headActive = false;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (controller.interpolationStopwatch >= 1.2f * controller.interpolationTime && !startedAnim) {
                startedAnim = true;
                // PlayAnimation("Body", "Throw", "Generic.playbackRate", duration);
                base.fixedAge = 0f;
            }

            if (anim.CheckEvent("Event.throw") && NetworkServer.active) {
                seat.additionalExitVelocity = (-controller.head.up).normalized * 15f;
                seat.EjectPassenger();
            }

            if (base.fixedAge >= duration && startedAnim) {
                outer.SetNextStateToMain();
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death;
        }
    }

    public class ThrowEnemySkill : SkillBase<ThrowEnemySkill>
    {
        public override string Name => "";

        public override string Description => "";

        public override Type ActivationStateType => typeof(ThrowEnemy);

        public override string ActivationMachineName => "Weapon";

        public override float Cooldown => 1f;

        public override Sprite Icon => null;
        public override bool BeginCooldownOnSkillEnd => true;
        public override bool CanceledFromSprinting => false;
    }
}