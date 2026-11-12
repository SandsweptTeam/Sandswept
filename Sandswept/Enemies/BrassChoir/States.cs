using System;
using System.Collections;
using R2API.Utils;

namespace Sandswept.Enemies.BrassChoir
{
    public class SpawnState : GenericCharacterSpawnState {
        public override void OnEnter()
        {
            base.duration = 3f;
            base.OnEnter();
            AkSoundEngine.PostEvent(Events.Play_boss_spawn_rumble, base.gameObject);
            AkSoundEngine.PostEvent(Events.Play_majorConstruct_spawn_rumble, base.gameObject);
            AkSoundEngine.PostEvent(Events.Play_bellBody_spawn, base.gameObject);
            AkSoundEngine.PostEvent(Events.Play_bellBody_spawn, base.gameObject);

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
        public static GameObject DeathEffect => Paths.GameObject.BellPreDeath;
        public override bool shouldAutoDestroy => false;

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

                RagdollController component = modelTransform.GetComponent<RagdollController>();
                Rigidbody component2 = GetComponent<Rigidbody>();
                if (component && component2)
                {
                    component.BeginRagdoll(component2.velocity * 1f);
                }

                modelTransform.gameObject.SetActive(false);
            }

            if (base.modelLocator)
            {
                base.modelLocator.autoUpdateModelTransform = false;
            }

            DestroyBodyAsapServer();
        }
    }
}