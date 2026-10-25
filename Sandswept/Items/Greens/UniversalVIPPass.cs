using LookingGlass.ItemStatsNameSpace;
using Sandswept.Items.NoTier;
using UnityEngine.SceneManagement;

namespace Sandswept.Items.Greens
{
    [ConfigSection("Items :: Universal VIP Pass")]
    public class UniversalVIPPass : ItemBase<UniversalVIPPass>
    {
        public override string ItemName => "Universal VIP Pass";

        public override string ItemLangTokenName => "UNIVERSAL_VIP_PASS";

        public override string ItemPickupDesc => "The first few chests every stage offer a discount and choice of item.";

        public override string ItemFullDescription => $"The first <style=cIsUtility>1</style> <style=cStack>(+1 per stack)</style> chests you open each stage have a <style=cIsDamage>{discount * 100f}% discount</style> and offer a <style=cIsUtility>choice</style> between <style=cIsDamage>{choices} items</style>.".AutoFormat();

        public override string ItemLore =>
        """
        "I'm sorry, sir, this is a restricted area. We can't allow you in."

        "Special orders from UES. It would be in your best interests to make an exception."

        "I'm afraid I can't do that. We received specific instructions from two of our guests not to let in anyone from UES. It'd be against policy to betray their trust."

        "Those two 'guests' have stolen from the UESC. You're harboring criminals. If you do not let us in, you will be obstructing justice in violation of interplanetary law."

        "The UESC does not have legal jurisdiction over Pluto, sir. We're under no obligation to let you in. If you don't vacate the premises, I'll be forced to call security, and make no mistake, our security is the best of the best."

        ...

        "...is that...?"

        "Yes. Universal. This is serious business. I'll ask one more time: let us through."

        "...right away, sir."
        """;
        public override ItemTier Tier => ItemTier.Tier2;

        public override GameObject ItemModel => Main.mainAssets.LoadAsset<GameObject>("UniVIPPrefab.prefab");

        public override Sprite ItemIcon => Main.mainAssets.LoadAsset<Sprite>("UniVIPIcon.png");

        [ConfigField("Discount", "Decimal", 0.5f)]
        public static float discount;
        [ConfigField("Choices", "Number", 2)]
        public static int choices;

        public static GameObject vfx;

        public static Color32 pink = new(229, 0, 218, 255);

        public override ItemTag[] ItemTags => [ItemTag.Utility, ItemTag.InteractableRelated, ItemTag.AIBlacklist, ItemTag.CannotCopy, ItemTag.BrotherBlacklist];

        public static int itemCount = 0;

        public override void Init()
        {
            base.Init();
            SetUpVFX();
        }

        public override object GetItemStatsDef()
        {
            ItemStatsDef itemStatsDef = new();
            //
            itemStatsDef.descriptions.Add("Uses: ");
            itemStatsDef.valueTypes.Add(ItemStatsDef.ValueType.Utility);
            itemStatsDef.measurementUnits.Add(ItemStatsDef.MeasurementUnits.Number);
            //
            itemStatsDef.descriptions.Add("Discount: ");
            itemStatsDef.valueTypes.Add(ItemStatsDef.ValueType.Damage);
            itemStatsDef.measurementUnits.Add(ItemStatsDef.MeasurementUnits.Percentage);
            //
            itemStatsDef.descriptions.Add("Choices: ");
            itemStatsDef.valueTypes.Add(ItemStatsDef.ValueType.Damage);
            itemStatsDef.measurementUnits.Add(ItemStatsDef.MeasurementUnits.Number);
            //
            itemStatsDef.calculateValuesNew = (luck, stack, procChance) =>
            {
                List<float> values = new()
                {
                    stack,
                    discount,
                    choices
                };

                return values;
            };

            return itemStatsDef;
        }

        public void SetUpVFX()
        {
            var uniVip = Main.mainAssets.LoadAsset<GameObject>("UniVIPPrefab.prefab");
            var uniVipMat = uniVip.transform.GetChild(0).GetComponent<MeshRenderer>().material;
            uniVipMat.SetColor("_Color", new Color32(205, 205, 205, 249));

            vfx = PrefabAPI.InstantiateClone(Paths.GameObject.ShrineChanceDollUseEffect, "Universal VIP Paws VFX", false);

            var transform = vfx.transform;

            var coloredLightShafts = transform.Find("ColoredLightShafts").GetComponent<ParticleSystemRenderer>();
            //
            var newColoredLightShaftMat = new Material(Paths.Material.matClayBossLightshaft);
            newColoredLightShaftMat.SetFloat("_AlphaBoost", 4f);
            newColoredLightShaftMat.SetFloat("_AlphaBias", 0.1f);
            newColoredLightShaftMat.SetColor("_TintColor", new Color32(255, 84, 0, 255));

            coloredLightShafts.material = newColoredLightShaftMat;

            transform.Find("ColoredLightShaftsBalance").GetComponent<ParticleSystemRenderer>().material = newColoredLightShaftMat;

            var coloredDustBalance = transform.Find("ColoredDustBalance").GetComponent<ParticleSystemRenderer>();
            var tex = Main.cursedConfig.Value ? Main.hifuSandswept.LoadAsset<Texture2D>("texPawMask.png") : Paths.Texture2D.texSpark1Mask;
            var newColoredDustBalanceMat = new Material(Paths.Material.matChanceShrineDollEffect);
            newColoredDustBalanceMat.SetColor("_TintColor", pink);
            newColoredDustBalanceMat.SetTexture("_RemapTex", Paths.Texture2D.texRampAreaIndicator);
            newColoredDustBalanceMat.SetFloat("_Boost", 12f);
            newColoredDustBalanceMat.SetTexture("_MainTex", tex);

            VFXUtils.MultiplyScale(vfx, 3f);
            VFXUtils.MultiplyDuration(vfx, 1.5f);
            VFXUtils.AddLight(vfx, pink, 100f, 20f, 2f);

            coloredDustBalance.material = newColoredDustBalanceMat;

            ContentAddition.AddEffect(vfx);
        }

        public override void Hooks()
        {
            On.RoR2.ChestBehavior.BaseItemDrop += AddOptions;
            On.RoR2.PurchaseInteraction.GetContextString += ShowDiscount;
            On.RoR2.PurchaseInteraction.CanBeAffordedByInteractor += CheckDiscount;
            On.RoR2.PurchaseInteraction.OnInteractionBegin += ApplyDiscount;
            On.RoR2.PurchaseInteraction.UpdateHologramContent += ShowDiscount2;
        }

        private void ShowDiscount2(On.RoR2.PurchaseInteraction.orig_UpdateHologramContent orig, PurchaseInteraction self, GameObject hologramContentObject, Transform viewer)
        {
            orig(self, hologramContentObject, viewer);
            
            if (self.costType == CostTypeIndex.Money && self.GetComponent<ChestBehavior>() && hologramContentObject.TryGetComponent<CostHologramContent>(out var component)) {
                if (self.viewerBody && self.viewerBody.inventory.GetItemCountPermanent(ItemDef) > 0) {
                    int oldCost = component.displayValue;
                    int cost = (int)Math.Floor(component.displayValue * (1f - discount));
                    
                    CostHologramContent.sharedStringBuilder.Clear();
                    Color color = Color.white;
                    CostTypeDef costTypeDef = CostTypeCatalog.GetCostTypeDef(self.costType);

                    if (costTypeDef != null)
                    {
                        CostHologramContent.sharedStringBuilder.Append("<style=cDeath><s>");
                        costTypeDef.BuildCostStringStyled(component.displayValue, CostHologramContent.sharedStringBuilder, forWorldDisplay: true, includeColor: false);
                        CostHologramContent.sharedStringBuilder.Append("</s></style> ");
                        costTypeDef.BuildCostStringStyled(cost, CostHologramContent.sharedStringBuilder, forWorldDisplay: true, includeColor: false);
                        color = costTypeDef.GetCostColor(forWorldDisplay: true);
                    }

                    component.targetTextMesh.SetText(CostHologramContent.sharedStringBuilder);
                    component.targetTextMesh.color = color;
                }
            }
        }

        private void ApplyDiscount(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
        {
            int oldCost = self.cost;

            if (self.GetComponent<ChestBehavior>() && self.costType == CostTypeIndex.Money && activator != null && activator.GetComponent<CharacterBody>() != null && activator.GetComponent<CharacterBody>().inventory.GetItemCountPermanent(ItemDef) > 0) {
                self.cost = (int)Math.Floor(self.cost * (1f - discount));     
            }

            orig(self, activator);

            self.cost = oldCost;
        }

        private bool CheckDiscount(On.RoR2.PurchaseInteraction.orig_CanBeAffordedByInteractor orig, PurchaseInteraction self, Interactor activator)
        {
            if (self.GetComponent<ChestBehavior>() && self.costType == CostTypeIndex.Money && activator != null && activator.GetComponent<CharacterBody>() != null && activator.GetComponent<CharacterBody>().inventory.GetItemCountPermanent(ItemDef) > 0) {
                int num = TeamManager.AdjustCostForLongstandingSolitude(self.costType, (int)Math.Floor(self.cost * (1f - discount)), activator.GetComponent<CharacterBody>());
		        return CostTypeCatalog.GetCostTypeDef(self.costType).IsAffordable(num, activator);
            }
            else {
                return orig(self, activator);
            }
        }

        private string ShowDiscount(On.RoR2.PurchaseInteraction.orig_GetContextString orig, PurchaseInteraction self, Interactor activator)
        {
            if (self.GetComponent<ChestBehavior>() && self.costType == CostTypeIndex.Money && activator != null && activator.GetComponent<CharacterBody>() != null && activator.GetComponent<CharacterBody>().inventory.GetItemCountPermanent(ItemDef) > 0) {
                PurchaseInteraction.sharedStringBuilder.Clear();
                PurchaseInteraction.sharedStringBuilder.Append(Language.GetString(self.contextToken));
                if (self.costType != 0)
                {
                    PurchaseInteraction.sharedStringBuilder.Append(" <nobr>(<style=cDeath><s>");
                    int num = TeamManager.AdjustCostForLongstandingSolitude(self.costType, self.cost);
                    CostTypeCatalog.GetCostTypeDef(self.costType).BuildCostStringStyled(num, PurchaseInteraction.sharedStringBuilder, forWorldDisplay: false);
                    PurchaseInteraction.sharedStringBuilder.Append("</s></style> ");
                    CostTypeCatalog.GetCostTypeDef(self.costType).BuildCostStringStyled((int)Math.Floor(num * (1f - discount)), PurchaseInteraction.sharedStringBuilder, forWorldDisplay: false);
                    PurchaseInteraction.sharedStringBuilder.Append(")</nobr>");
                }

                return PurchaseInteraction.sharedStringBuilder.ToString();
            }
            else {
                return orig(self, activator);
            }
        }

        private void AddOptions(On.RoR2.ChestBehavior.orig_BaseItemDrop orig, ChestBehavior self)
        {
            PurchaseInteraction purchase = self.GetComponent<PurchaseInteraction>();

            if (!purchase || purchase.costType != CostTypeIndex.Money) {
                orig(self);
                return;
            }
        
            if (!purchase.lastActivator || !purchase.lastActivator.GetComponent<CharacterBody>()) {
                orig(self);
                return;
            }

            CharacterBody body = purchase.lastActivator.GetComponent<CharacterBody>();

            if (body.inventory && body.inventory.GetItemCountPermanent(ItemDef.itemIndex) > 0) {
                Inventory.ItemTransformation itemTransformation = default(Inventory.ItemTransformation);
                itemTransformation.originalItemIndex = ItemDef.itemIndex;
                itemTransformation.newItemIndex = UniversalVIPPassSpent.instance.ItemDef.itemIndex;
                itemTransformation.maxToTransform = 1;
                itemTransformation.transformationType = (ItemTransformationTypeIndex)5;
                Inventory.ItemTransformation itemTransformation2 = itemTransformation;
                itemTransformation2.TryTransform(body.inventory, out var _);

                Util.PlaySound("Play_UI_commandHUD_select", self.gameObject);
                Util.PlaySound("Play_UI_commandHUD_select", self.gameObject);

                EffectManager.SpawnEffect(vfx, new EffectData
                {
                    origin = self.transform.position,
                    rotation = Quaternion.identity,
                    scale = 3f,
                    color = pink
                }, true);

                Util.PlaySound("Play_UI_obj_casinoChest_open", self.gameObject);
                Util.PlaySound("Play_ui_obj_lunarPool_activate", self.gameObject);

                if (self.currentPickup.isValid && self.dropCount >= 1)
                {
                    ItemDef item = ItemCatalog.GetItemDef(PickupCatalog.GetPickupDef(self.currentPickup.pickupIndex).itemIndex);

                    float angle = 360f / self.dropCount;
                    Vector3 vector = Vector3.up * self.dropUpVelocityStrength + self.dropTransform.forward * self.dropForwardVelocityStrength;
                    Quaternion quaternion = Quaternion.AngleAxis(angle, Vector3.up);
                    
                    for (int i = 0; i < self.dropCount; i++)
                    {
                        GenericPickupController.CreatePickupInfo createPickupInfo = default(GenericPickupController.CreatePickupInfo);
                        createPickupInfo.pickupIndex = PickupCatalog.FindPickupIndex(item.tier);
                        createPickupInfo.position = self.dropTransform.position + Vector3.up * 1.5f;
                        createPickupInfo.chest = self;
                        createPickupInfo.prefabOverride = Paths.GameObject.OptionPickup;

                        createPickupInfo.pickerOptions = new PickupPickerController.Option[choices];

                        for (int j = 0; j < choices; j++) {
                            createPickupInfo.pickerOptions[j] = new PickupPickerController.Option() {
                                pickupIndex = self.dropTable.GeneratePickup(self.rng).pickupIndex,
                                available = true
                            };
                        }
                        
                        GenericPickupController.CreatePickupInfo pickupInfo = createPickupInfo;
                        PickupDropletController.CreatePickupDroplet(pickupInfo, pickupInfo.position, vector);

                        vector = quaternion * vector;
                    }

                    self.currentPickup = UniquePickup.none;
                    self.NetworkisChestOpened = true;
                }
            }
            else {
                orig(self);
            }
        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {

            var itemDisplay = SetUpIDRS();

            ItemDisplayRuleDict i = new();

            #region Sandswept Survivors
            /*
            i.Add("RangerBody",

                new ItemDisplayRule()
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.00387F, 0.11857F, 0.01629F),
                    localAngles = new Vector3(84.61184F, 220.3867F, 47.41245F),
                    localScale = new Vector3(0.14531F, 0.14659F, 0.14531F),

                    followerPrefab = itemDisplay,
                    limbMask = LimbFlags.None,
                    followerPrefabAddress = new AssetReferenceGameObject("")
                }

            );
            */

            i.Add("ElectricianBody",

                new ItemDisplayRule()
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    childName = "FootR",
                    localPos = new Vector3(0.06724F, -0.05793F, -0.01421F),
                    localAngles = new Vector3(333.9098F, 98.90534F, 162.9532F),
                    localScale = new Vector3(0.11715F, 0.10939F, 0.10939F),

                    followerPrefab = itemDisplay,
                    limbMask = LimbFlags.None,
                    followerPrefabAddress = new AssetReferenceGameObject("")
                }

            );

            #endregion

            return i;

        }
    }
}