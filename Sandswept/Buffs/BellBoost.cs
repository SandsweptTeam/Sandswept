using System;
using System.Collections.Generic;
using System.Text;

namespace Sandswept.Buffs
{
    [ConfigSection("Enemies :: Brass Choir")]
    public class BellBoost : BuffBase<BellBoost>
    {
        public override string BuffName => "Bell Boost";

        public override Color Color => new Color32(155, 132, 62, 255);

        public override Sprite BuffIcon => Main.assets.LoadAsset<Sprite>("texBuffThetaConstruct.png");

        public override bool CanStack => false;
        public override bool IsDebuff => false;

        [ConfigField("Bell Buff Attack Speed Gain", "", 0.6f)]
        public static float attackSpeedGain;

        [ConfigField("Bell Buff Cooldown Reduction", "Decimal.", 0.6f)]
        public static float cooldownReduction;
        [ConfigField("Bell Buff Cooldown Reduction", "Decimal.", 0.6f)]
        public static float movementSpeed;
        public static Material matBellBoost;

        public override void Init()
        {
            base.Init();

            RecalculateStatsAPI.GetStatCoefficients += HandleSpeedBuff;
            On.RoR2.CharacterModel.UpdateOverlays += UpdateOverlays;

            matBellBoost = Object.Instantiate(Paths.Material.matBellBuff);
            GameObject.DontDestroyOnLoad(matBellBoost);
        }

        private void UpdateOverlays(On.RoR2.CharacterModel.orig_UpdateOverlays orig, CharacterModel self)
        {
            orig(self);

            AddOverlay(matBellBoost, self.body && self.body.GetBuffCount(BuffDef) > 0);

            void AddOverlay(Material overlayMaterial, bool condition)
            {
                if (self.activeOverlayCount < CharacterModel.maxOverlays && condition)
                {
                    self.currentOverlays[self.activeOverlayCount++] = overlayMaterial;
                }
            }
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