using System;

namespace Sandswept.Enemies.Ivy
{
    public class IvyDeathState : GenericCharacterDeath
    {
        public override void CreateDeathEffects()
        {
            base.CreateDeathEffects();

            GetModelTransform().GetComponent<IvyModelController>().enabled = false;
            PlayAnimation("Body", "Death", "Generic.playbackRate", 2f);
        }
    }
}