using System;

namespace Sandswept.Enemies.StoneFragment.States {
    public class DeathState : GenericCharacterDeath
    {
        public static LazyAddressable<GameObject> initialEffect => new(() => Paths.GameObject.ExplosionMinorConstruct);

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

                modelTransform.gameObject.SetActive(false);
    
            }
            if (base.modelLocator)
            {
                base.modelLocator.autoUpdateModelTransform = false;
            }
            // DestroyBodyAsapServer();
        }
    }
}