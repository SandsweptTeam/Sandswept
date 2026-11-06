using System;

namespace Sandswept.Enemies.StoneFragment.States {
    public class DeathState : GenericCharacterDeath
    {
        public static LazyAddressable<GameObject> initialEffect => new(() => Paths.GameObject.ExplosionGolemDeath);

        public static float initialEffectScale;

        public override bool shouldAutoDestroy => true;

        public override void OnEnter()
        {
            base.OnEnter();
            Transform modelTransform = GetModelTransform();
            if (modelTransform)
            {
                if (modelTransform.GetComponent<ChildLocator>())
                {
                    EffectManager.SpawnEffect(initialEffect, new EffectData
                    {
                        origin = base.transform.position,
                        scale = initialEffectScale
                    }, transmit: false);
                }

                RagdollController component = modelTransform.GetComponent<RagdollController>();
                Rigidbody component2 = GetComponent<Rigidbody>();
                if (component && component2)
                {
                    component.BeginRagdoll(component2.velocity * 1f);
                }
    
            }
            if (base.modelLocator)
            {
                base.modelLocator.autoUpdateModelTransform = false;
            }
            // DestroyBodyAsapServer();
        }
    }
}