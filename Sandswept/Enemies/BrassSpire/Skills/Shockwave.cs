using System;
using System.Collections;
using System.Diagnostics.Contracts;
using System.Linq;
using Sandswept.Enemies.BrassBell;
using Sandswept.Enemies.BrassSpire;
using Sandswept.Survivors;
using static RoR2.MasterCatalog;

namespace Sandswept.Enemies.BrassSpire.States {
    public class Shockwave : BaseSkillState {
        public int shockwaveCount = 8;
        public float damageCoefficient = 2f;
        public static LazyAddressable<GameObject> SlamEffect = new(() => Paths.GameObject.TotemGroundSlamEffect);

        public override void OnEnter()
        {
            base.OnEnter();

            outer.SetNextStateToMain();
        }

        public override void OnExit()
        {
            base.OnExit();

            if (NetworkServer.active) {
                FireProjectileInfo info = new();
                info.position = base.characterBody.footPosition;
                info.damage = base.damageStat;
                info.owner = base.gameObject;
                info.projectilePrefab = BrassSpire.ShockwaveProjectile;
                info.rotation = Quaternion.identity;

                ProjectileManager.instance.FireProjectile(info);
                
                EffectManager.SpawnEffect(SlamEffect, new EffectData() {
                    origin = base.characterBody.footPosition,
                    rotation = Quaternion.identity,
                    scale = 1f
                }, true);
            }
        }
    }

    public class ShockwaveSkill : SkillBase<ShockwaveSkill> {
        public override string Name => "";

        public override string Description => "";

        public override Type ActivationStateType => typeof(Shockwave);

        public override string ActivationMachineName => "Weapon";

        public override float Cooldown => 1f;

        public override Sprite Icon => null;
        public override bool BeginCooldownOnSkillEnd => true;
        public override bool CanceledFromSprinting => false;
        public override bool FullRestockOnAssign => false;
    }
}