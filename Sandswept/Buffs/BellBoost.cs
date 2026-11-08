using System;
using System.Collections.Generic;
using System.Text;

namespace Sandswept.Buffs
{
    [ConfigSection("Enemies :: Brass Choir")]
    public class BellBoost : BuffBase<BellBoost>
    {
        public override string BuffName => "Bell Boost";

        public override Color Color => Color.white;

        public override Sprite BuffIcon => null;

        public override bool CanStack => false;
        public override bool IsDebuff => false;

        [ConfigField("Bell Buff Attack Speed Gain", "", 0.2f)]
        public static float attackSpeedGain;

        [ConfigField("Bell Buff Cooldown Reduction", "Decimal.", 0.2f)]
        public static float cooldownReduction;
        [ConfigField("Bell Buff Cooldown Reduction", "Decimal.", 0.4f)]
        public static float movementSpeed;

        public override void Init()
        {
            base.Init();

            RecalculateStatsAPI.GetStatCoefficients += HandleSpeedBuff;
        }

        private void HandleSpeedBuff(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender.HasBuff(BuffDef))
            {
                int c = sender.GetBuffCount(BuffDef);
                if (c > 0) {
                    args.attackSpeedMultAdd += attackSpeedGain;
                    args.cooldownMultAdd -= cooldownReduction;
                    args.moveSpeedMultAdd += movementSpeed;
                }
            }
        }
    }
}