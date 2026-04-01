using System;

namespace Sandswept.Enemies.Ivy
{
    public class IvySpawnState : BaseState
    {
        public float duration = 1.1f;
        public override void OnEnter()
        {
            base.OnEnter();
            GetModelTransform().GetComponent<IvyModelController>().enabled = false;
            // PlayAnimation("Override, Head", "Idle");
            PlayAnimation("Body", "Spawn", "Generic.playbackRate", duration);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= duration)
            {
                outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            base.OnExit();
            GetModelTransform().GetComponent<IvyModelController>().enabled = true;
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death;
        }
    }
}