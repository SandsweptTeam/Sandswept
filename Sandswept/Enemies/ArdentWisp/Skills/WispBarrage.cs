using System;
using Sandswept.Survivors;

namespace Sandswept.Enemies.ArdentWisp.States {
    public class WispBarrageWindUp : BaseSkillState {
        public float duration = 0.8f;
        public override void OnEnter()
        {
            base.OnEnter();

            PlayAnimation("Body", "Wind Barrage", "Generic.playbackRate", duration * 1.2f);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= duration) {
                outer.SetNextState(new WispBarrage());
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Stun;
        }
    }

    public class WispBarrage : BaseSkillState {
        public float duration = 3f;
        public float stopwatch = 0f;
        public float fireRate = 0f;
        public float shotsPerSec = 3f;
        public float damage = 0.5f;
        public GameObject prefab;
        public Transform muzzle;
        public override void OnEnter()
        {
            base.OnEnter();

            fireRate = 1f / shotsPerSec;
            fireRate /= base.attackSpeedStat;

            prefab = ArdentWisp.WispBarrageShot;

            GetModelAnimator().SetBool("endBarrage", false);

            PlayAnimation("Body", "Loop Barrage");

            muzzle = FindModelChild("MuzzleCenter");
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            stopwatch += Time.fixedDeltaTime;

            base.rigidbody.velocity = Vector3.zero;

            if (stopwatch >= fireRate) {
                stopwatch = 0f;

                if (base.isAuthority) {
                    FireProjectileInfo info = new();
                    info.projectilePrefab = prefab;
                    info.position = muzzle.position;
                    info.rotation = Util.QuaternionSafeLookRotation(Util.ApplySpread(muzzle.forward, -0.7f, 0.7f, 1f, 1f));
                    info.damage = base.damageStat * damage;
                    info.owner = base.gameObject;
                    ProjectileManager.instance.FireProjectile(info);
                }
            }

            if (base.fixedAge >= duration) {
                outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            base.OnExit();

            GetModelAnimator().SetBool("endBarrage", true);
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Stun;
        }
    }

    public class WispBarrageSkill : SkillBase<WispBarrageSkill>
    {
        public override string Name => "omg hiii";

        public override string Description => "<3 :3 :3 <3 UwU >w< >_< >_> OwO :3 <3";

        public override Type ActivationStateType => typeof(WispBarrageWindUp);

        public override string ActivationMachineName => "Body";

        public override float Cooldown => 7f;

        public override Sprite Icon => null;
        public override bool BeginCooldownOnSkillEnd => true;
        public override bool CanceledFromSprinting => false;
    }
}