using System;
using System.Linq;
using Sandswept.Survivors;

namespace Sandswept.Enemies.Ivy {
    public class DeployHead : BaseSkillState {
        public float duration = 0.55f;
        public override void OnEnter()
        {
            base.OnEnter();

            if (NetworkServer.active) {
                IvyMainState main = EntityStateMachine.FindByCustomName(base.gameObject, "Body").state as IvyMainState;
                TeleportHelper.TeleportBody(main.IvyHeadBody, GetComponent<VehicleSeat>().seatPosition.position);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= duration) {
                if (NetworkServer.active) {
                    IvyMainState main = EntityStateMachine.FindByCustomName(base.gameObject, "Body").state as IvyMainState;
                    main.IvyHeadBody.master.aiComponents[0].customTarget.gameObject = base.characterBody.master.aiComponents[0].customTarget.gameObject;
                    main.controller.headActive = true;
                    base.characterMotor.walkSpeedPenaltyCoefficient = 0f;
                }
            
                outer.SetNextStateToMain();
            }
        }
    }

    public class GrabTarget : BaseSkillState {
        public override void OnEnter()
        {
            base.OnEnter();

            if (NetworkServer.active) {
                base.characterBody.master.minionOwnership.ownerMaster.GetBody().GetComponent<VehicleSeat>().SetPassenger(base.characterBody.master.aiComponents[0].customTarget.gameObject);
                base.characterBody.master.aiComponents[0].customTarget.gameObject = null;
                base.characterBody.master.minionOwnership.ownerMaster.GetBody().characterMotor.walkSpeedPenaltyCoefficient = 1f;
                TeleportHelper.TeleportBody(base.characterBody, base.characterBody.footPosition + (Vector3.up * 10f));
            }
            
            outer.SetNextStateToMain();
        }
    }

    public class DeployHeadSkill : SkillBase<DeployHeadSkill>
    {
        public override string Name => "";

        public override string Description => "";

        public override Type ActivationStateType => typeof(DeployHead);

        public override string ActivationMachineName => "Weapon";

        public override float Cooldown => 10f;

        public override Sprite Icon => null;
        public override bool BeginCooldownOnSkillEnd => true;
        public override bool CanceledFromSprinting => false;
    }

    public class GrabTargetSkill : SkillBase<GrabTargetSkill>
    {
        public override string Name => "";

        public override string Description => "";

        public override Type ActivationStateType => typeof(GrabTarget);

        public override string ActivationMachineName => "Weapon";

        public override float Cooldown => 10f;

        public override Sprite Icon => null;
        public override bool BeginCooldownOnSkillEnd => true;
        public override bool CanceledFromSprinting => false;
    }
}