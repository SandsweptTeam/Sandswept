using EntityStates.Bandit2;
using static Sandswept.Main;

namespace Sandswept.Equipment.Standard
{
    [ConfigSection("Equipment :: Equine Electrolyte Blend")]
    public class HorseBlend : EquipmentBase<HorseBlend>
    {
        public override string EquipmentName => "Equine Electrolyte Blend";

        public override string EquipmentLangTokenName => "HORSEMIX";

        public override string EquipmentPickupDesc => "Gain a massive increase to movement speed, damage, armor, and dehydration rate.";

        public override string EquipmentFullDescription => $"Increase <style=cIsDamage>damage</style> and <style=cIsUtility>movement speed</style> by <style=cIsDamage>100%</style> for a short time. Increase <style=cIsHealing>armor</style> by <style=cIsDamage>100</style>. Increase <style=cIsHealth>health regeneration</style> by <style=cIsDamage>-10% per second</style>.".AutoFormat();

        public override string EquipmentLore =>
        """
        UltraCruz Equine Electrolyte Supplement for Horses is a comprehensive blend of important macro- and micro-minerals designed to restore electrolyte levels in horses after vigorous exercise or during hot weather. Horses lose electrolytes during work, mainly in the form of sweat. A horse that sweats a lot during hot weather or exercise is at risk for a negative electrolyte balance because of the mineral loss that can occur. Supplementing electrolytes helps to replenish electrolytes lost during strenuous exercise, supports muscle development and post-exercise recovery, and may also promote proper hydration by stimulating thirst. Developed and manufactured in the USA with globally-sourced ingredients by Santa Cruz Animal Health.

        - Designed for horses that sweat excessively during hot weather or rigorous exercise and helps stimulate thirst in animals not drinking enough water
        - Replenishes electrolytes and nutrients lost during heavy exercise
        - Contains a balance of critical electrolytes including Sodium, Magnesium, Calcium and other trace minerals
        - Provided as either powder or paste
        """;

        public override GameObject EquipmentModel => Main.assets.LoadAsset<GameObject>("DisplayHorseBlend.prefab");

        public override Sprite EquipmentIcon => Main.assets.LoadAsset<Sprite>("texHorseBlend.png");
        public override float Cooldown => 55f;

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return new ItemDisplayRuleDict();
        }

        public override void Init()
        {
            if (cursedConfig.Value) {
                base.Init();
            }
        }

        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            if (slot.characterBody == null)
            {
                return false;
            }

            slot.characterBody.AddTimedBuff(Buffs.Horsepower.instance.BuffDef, 10f);

            return true;
        }
    }
}