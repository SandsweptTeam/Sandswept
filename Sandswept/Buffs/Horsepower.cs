using System;
using System.Collections.Generic;
using System.Text;

namespace Sandswept.Buffs
{
    public class Horsepower : BuffBase<Horsepower>
    {
        public override string BuffName => "Horsepower";

        public override Color Color => Color.gray;

        public override Sprite BuffIcon => Main.assets.LoadAsset<Sprite>("Horsepower.png");

        public override bool CanStack => false;
        public override bool IsDebuff => false;

        public override void Init()
        {
            base.Init();

            RecalculateStatsAPI.GetStatCoefficients += HandleSpeedBuff;
        }

        private void HandleSpeedBuff(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender.HasBuff(BuffDef))
            {
                args.damageMultAdd += 1f;
                args.moveSpeedMultAdd += 1f;
                args.armorAdd += 100;
                args.baseRegenAdd += sender.healthComponent.fullHealth * -0.10f;
            }
        }
    }
}