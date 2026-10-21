using System;
using R2API.Utils;

namespace Sandswept.Enemies
{
    public class BaseConstructDeath : GenericCharacterDeath
    {
        public static LazyAddressable<GameObject> DeathEffect = new(() => Paths.GameObject.ExplosionMinorConstruct);

        public override void CreateDeathEffects()
        {
            base.CreateDeathEffects();

            characterBody.modelLocator.autoUpdateModelTransform = false;
            cachedModelTransform.parent = null;

            var boxes = cachedModelTransform.GetComponentsInChildren<HurtBox>(true);
            for (int i = 0; i < boxes.Length; i++)
            {
                boxes[i].enabled = false;
                boxes[i].gameObject.SetActive(false);
            }
            cachedModelTransform.GetComponent<RagdollController>().BeginRagdoll(Vector3.one * -5f);

            EffectManager.SpawnEffect(DeathEffect, new EffectData
            {
                origin = base.characterBody.corePosition,
                scale = base.characterBody.bestFitRadius * 2f,
            }, false);

            Util.PlaySound("Play_minorConstruct_attack_explode", base.gameObject);
            ExpireRagdoll(cachedModelTransform);
        }

        public static float ragdollLifetime = 8f;

        // dying releases the model as a corpse that only gets cleaned up by the corpse limit, and construct ragdolls
        // start jittering and stretching after a while, so they get removed after a few seconds instead
        public static void ExpireRagdoll(Transform model)
        {
            if (!model)
            {
                return;
            }

            model.gameObject.AddComponent<DestroyOnTimer>().duration = ragdollLifetime;
        }
    }
}