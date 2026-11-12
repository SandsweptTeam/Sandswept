using System;
using System.Diagnostics.Contracts;
using Sandswept.Survivors;

namespace Sandswept.Enemies.BrassChoir.States {
    public class Barrage : BaseSkillState {
        public float damageCoeff = 0.8f;
        public string[] projectiles = new string[] {
            "Muzzle1", "Muzzle2", "Muzzle3", "Muzzle4", "Muzzle5", "Muzzle6"
        };

        public GameObject[] prepShots;
        public float fireTime = 0f;
        public int totalShots = 6;
        public bool naturalExit = false;

        public override void OnEnter()
        {
            base.OnEnter();

            prepShots = new GameObject[projectiles.Length];

            for (int i = 0; i < projectiles.Length; i++) {
                prepShots[i] = GameObject.Instantiate(BrassChoir.BellShotPrep, FindModelChild(projectiles[i]));
            }

            AkSoundEngine.PostEvent(Events.Play_bellBody_attackCreate, base.gameObject);

            if (base.isAuthority) {
                BrassChoir.ReplicateToSpires(base.characterBody, SkillSlot.Primary);
            }

            PlayAnimation("Body", "Prep Shoot");
            GetModelAnimator().SetBool("isShooting", true);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= 1f) {
                fireTime += Time.fixedDeltaTime;
            }

            if (base.fixedAge >= 1f && totalShots > 0 && fireTime >= 0.4f) {
                fireTime = 0f;
                totalShots--;

                GameObject.Destroy(prepShots[totalShots]);
                AkSoundEngine.PostEvent(Events.Play_bellBody_attackShoot, base.gameObject);

                PlayAnimation("Gesture, Additive", "Shoot");

                if (base.isAuthority) {
                    Vector3 point = base.GetAimRay().GetPoint(50f);
                    Vector3 pos = FindModelChild(projectiles[totalShots]).position;

                    FireProjectileInfo info = new();
                    info.owner = base.gameObject;
                    info.damage = base.damageStat * damageCoeff;
                    info.rotation = Util.QuaternionSafeLookRotation(
                        Util.ApplySpread((point - pos).normalized, -2f, 2f, 1f, 1f)
                    );
                    info.position = pos;
                    info.projectilePrefab = BrassChoir.BellShotProjectile;
                    info.crit = base.RollCrit();

                    ProjectileManager.instance.FireProjectile(info);
                }
            }
            
            if (totalShots <= 0) {
                outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            base.OnExit();

            GetModelAnimator().SetBool("isShooting", false);
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Stun;
        }
    }

    public class BarrageSkill : SkillBase<BarrageSkill> {
        public override string Name => "";

        public override string Description => "";

        public override Type ActivationStateType => typeof(Barrage);

        public override string ActivationMachineName => "Weapon";

        public override float Cooldown => 6f;

        public override Sprite Icon => null;
        public override bool BeginCooldownOnSkillEnd => true;
        public override bool CanceledFromSprinting => false;
    }
}