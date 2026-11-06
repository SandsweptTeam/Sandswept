using System;
using System.Diagnostics.Contracts;
using Sandswept.Survivors;

namespace Sandswept.Enemies.BrassBell.States {
    public class Barrage : BaseSkillState {
        public float damageCoeff = 0.8f;
        public string[] projectiles = new string[] {
            "Muzzle1", "Muzzle2", "Muzzle3", "Muzzle4"
        };

        public GameObject[] prepShots;

        public float duration = 1f;
        public bool naturalExit = false;

        public override void OnEnter()
        {
            base.OnEnter();

            duration /= base.attackSpeedStat;

            prepShots = new GameObject[projectiles.Length];

            for (int i = 0; i < projectiles.Length; i++) {
                prepShots[i] = GameObject.Instantiate(BrassBell.BellShotPrep, FindModelChild(projectiles[i]));
            }

            AkSoundEngine.PostEvent(Events.Play_bellBody_attackCreate, base.gameObject);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            
            if (base.fixedAge >= duration) {
                naturalExit = true;
                outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            base.OnExit();

            for (int i = 0; i < prepShots.Length; i++) {
                if (prepShots[i]) {
                    GameObject.Destroy(prepShots[i]);
                }
            }

            if (naturalExit) {
                AkSoundEngine.PostEvent(Events.Play_bellBody_attackShoot, base.gameObject);
            }

            PlayAnimation("Body", "Ring");

            if (base.isAuthority && naturalExit) {
                Vector3 point = base.GetAimRay().GetPoint(50f);

                foreach (string projectile in projectiles) {
                    Vector3 pos = FindModelChild(projectile).position;

                    FireProjectileInfo info = new();
                    info.owner = base.gameObject;
                    info.damage = base.damageStat * damageCoeff;
                    info.rotation = Util.QuaternionSafeLookRotation((point - pos).normalized);
                    info.position = pos;
                    info.projectilePrefab = BrassBell.BellShotProjectile;
                    info.crit = base.RollCrit();

                    ProjectileManager.instance.FireProjectile(info);
                }
            }
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

        public override float Cooldown => 5f;

        public override Sprite Icon => null;
        public override bool BeginCooldownOnSkillEnd => true;
        public override bool CanceledFromSprinting => false;
    }
}