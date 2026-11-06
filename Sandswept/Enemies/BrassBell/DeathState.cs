using System;

namespace Sandswept.Enemies.BrassBell.States {
    public class DeathState : GenericCharacterDeath
    {
        public static LazyAddressable<GameObject> initialEffect => new(() => Paths.GameObject.BellDeath);

        public static float initialEffectScale;

        public override bool shouldAutoDestroy => false;

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
            DestroyBodyAsapServer();
        }
    }
}