using System;
using System.Collections;
using R2API.Utils;

namespace Sandswept.Enemies.ArdentWisp
{
    public class ArdentWispSpawn : GenericCharacterSpawnState {
        public override void OnEnter()
        {
            base.duration = 2f;
            base.OnEnter();
            AkSoundEngine.PostEvent(Events.Play_greater_wisp_attack, base.gameObject);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
        }
    }
    public class ArdentWispDeath : GenericCharacterDeath
    {
        public static GameObject DeathEffect => ArdentWisp.ArdentDeathEffect;
        public static int DeathFireballCount = 12;

        public override void OnEnter()
        {
            base.OnEnter();

            base.GetModelTransform().gameObject.SetActive(false);
            base.gameObject.layer = LayerIndex.noCollision.intVal;

            base.characterBody.StartCoroutine(Explosion());

            EffectManager.SpawnEffect(DeathEffect, new EffectData
            {
                origin = base.characterBody.corePosition,
                scale = 4f,
            }, false);

            Util.PlaySound("Play_minorConstruct_attack_explode", base.gameObject);
        }

        public IEnumerator Explosion() {
            yield return new WaitForEndOfFrame();
            if (base.isAuthority) {
                for (int i = 0; i < DeathFireballCount; i++) {
                    Vector3 dir = Random.onUnitSphere.normalized;
                    FireProjectileInfo info = new();
                    info.projectilePrefab = ArdentWisp.WispBarrageShot;
                    info.position = base.characterBody.corePosition + (1f * dir);
                    info.rotation = Util.QuaternionSafeLookRotation(dir);
                    info.damage = base.damageStat * 0.5f;
                    info.owner = base.gameObject;
                    ProjectileManager.instance.FireProjectile(info);
                    yield return new WaitForEndOfFrame();
                }
            }
        }
    }
}