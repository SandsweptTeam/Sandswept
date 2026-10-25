using LookingGlass.ItemStatsNameSpace;
using Sandswept.Items.Greens;

namespace Sandswept.Items.NoTier
{
    internal class UniversalVIPPassSpent : ItemBase<UniversalVIPPassSpent>
    {
        public override string ItemName => "Universal VIP Pass (Consumed)";

        public override string ItemLangTokenName => "VIP_PASS_SPENT";

        public override string ItemPickupDesc => "A spent pass that has already provided its worth. At the start of each stage, transforms into Universal VIP Pass.";

        public override string ItemFullDescription => "A spent pass that has already provided its worth. At the start of each stage, transforms into Universal VIP Pass.".AutoFormat();

        public override string ItemLore => "OwO UwU :333 >w< >_< <3 uwuuu owo <3 <3";

        public override ItemTier Tier => ItemTier.NoTier;

        public override GameObject ItemModel => Paths.GameObject.DisplayEliteBeadSpike;

        public override Sprite ItemIcon => Main.assets.LoadAsset<Sprite>("texPassSpent.png");

        public override ItemTag[] ItemTags => [];

        public override bool CanRemove => false;

        public override void Init()
        {
            base.Init();
        }

        public override void Hooks()
        {
            On.RoR2.CharacterMaster.TryRegenerateScrap += RegenPass;
        }

        private void RegenPass(On.RoR2.CharacterMaster.orig_TryRegenerateScrap orig, RoR2.CharacterMaster self)
        {
            orig(self);

            Inventory.ItemTransformation itemTransformation = default(Inventory.ItemTransformation);
            itemTransformation.originalItemIndex = ItemDef.itemIndex;
            itemTransformation.newItemIndex = UniversalVIPPass.instance.ItemDef.itemIndex;
            itemTransformation.maxToTransform = int.MaxValue;
            itemTransformation.transformationType = (ItemTransformationTypeIndex)5;
            Inventory.ItemTransformation itemTransformation2 = itemTransformation;
            itemTransformation2.TryTransform(self.inventory, out var _);
        }

        public override object GetItemStatsDef()
        {
            ItemStatsDef itemStatsDef = new();
            return itemStatsDef;
        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return new ItemDisplayRuleDict();
        }
    }
}