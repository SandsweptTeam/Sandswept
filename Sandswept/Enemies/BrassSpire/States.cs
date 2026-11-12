using System;
using System.Collections;
using R2API.Utils;

namespace Sandswept.Enemies.BrassSpire
{
    public class SpawnState : GenericCharacterSpawnState {
        public override void OnEnter()
        {
            base.duration = 3f;
            base.OnEnter();

            PlayAnimation("Body", "Spawn");
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= duration) {
                outer.SetNextStateToMain();
            }
        }
    }
    public class DeathState : GenericCharacterDeath
    {
        public static LazyAddressable<GameObject> DeathEffect => new(() => Paths.GameObject.HermitCrabBombExplosion);
        public override bool shouldAutoDestroy => true;

        public override void OnEnter()
        {
            base.OnEnter();

            Transform modelTransform = GetModelTransform();

            if (modelTransform)
            {
                if (modelTransform.GetComponent<ChildLocator>())
                {
                    EffectManager.SpawnEffect(DeathEffect, new EffectData
                    {
                        origin = base.transform.position,
                        scale = 3f
                    }, transmit: false);
                }
            }

            if (base.modelLocator)
            {
                base.modelLocator.autoUpdateModelTransform = false;
            }

            base.DestroyModel();

            DestroyBodyAsapServer();
        }
    }
}