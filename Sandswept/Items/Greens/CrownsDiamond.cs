using LookingGlass.ItemStatsNameSpace;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace Sandswept.Items.Greens
{
    [ConfigSection("Items :: Crowns Diamond")]
    public class CrownsDiamond : ItemBase<CrownsDiamond>
    {
        public override string ItemName => "Crown's Diamond";

        public override string ItemLangTokenName => "CROWNS_DIAMOND";

        public override string ItemPickupDesc => "Entering combat grants barrier. Strengthen you and nearby allies while you have an active barrier.";

        public override string ItemFullDescription => $"Entering combat grants a <style=cIsHealing>temporary barrier</style> for <style=cIsHealing>{barrierGain * 100f}% maximum health</style>. While you have an active <style=cIsHealing>barrier</style>, project a ward that grants allies <style=cIsDamage>+{damageGain * 100f}% damage</style> <style=cStack>(+{damageGain * 100f}% per stack)</style> and <style=cIsDamage>+{attackSpeedGain * 100f}% attack speed</style> <style=cStack>(+{attackSpeedGain * 100f}% per stack)<style>".AutoFormat();

        public override string ItemLore =>
        """
        Order: Faux Cut Opal
        Tracking Number: 443*****
        Estimated Delivery: 7/01/2026
        Shipping Method: Top Priority
        Shipping Address: Aganta Palace, Mercury
        Shipping Details:

        The Venusians know that Mercury is responsible for the assassination, and are planning a counterattack. They have tried to keep it under wraps, of course, but Your Majesty's intelligence proves its excellence once again, and we remain a step ahead. Intelligence has heard whispers that Venus has now acquired one of the Neptunian ritual knives, as well, and are now developing ultra-phasic shielding of their own -- though I cannot yet confirm this.

        Your Majesty's appointed Master of Strategy worries that striking first to stop their inevitable counterattack may lead to outright war, and suggests Your Majesty increase defensive measures instead. The Venusians are planning to send a solitary assassin, as we did, in order to infiltrate Mercury easily and avoid blame. Of course, it is Your Majesty's decision -- but if you should accept the Master of Strategy's proposal, she suggests you make use of this trinket.

        As Your Majesty will see, it is a near-perfect replica of the frontmost Agantan Crown diamond, meant to temporarily replace it. Within it is an ultra-phasic shield generator, which will provide protection in the event the assassin is able to reach the Palace. This new technology is unknown to Venus, and beyond them, no one knows of ultra-phasic shielding; it will be not only unexpected, but invisible, requiring no change in Your Majesty's routine or appearance to be effective.

        If Your Majesty would like to proceed with this plan, we have similar diamonds for every other in the Agantan Crown, providing Your Majesty with unparalleled protection. Should I or the Master of Strategy receive word, we will send them to the Palace post-haste.

        For the glory of Immortal Mercury.
        """;
        [ConfigField("Barrier Gain", "Decimal.", 0.3f)]
        public static float barrierGain;
        [ConfigField("Attack Speed Gain", "Decimal.", 0.25f)]
        public static float attackSpeedGain;
        [ConfigField("Damage Gain", "Decimal.", 0.25f)]
        public static float damageGain;

        public override ItemTier Tier => ItemTier.Tier2;

        public override GameObject ItemModel => Main.hifuSandswept.LoadAsset<GameObject>("CrownsDiamondHolder.prefab");

        public override Sprite ItemIcon => Main.hifuSandswept.LoadAsset<Sprite>("texCrownsDiamond.png");

        public override ItemTag[] ItemTags => [ItemTag.Utility, ItemTag.Healing, ItemTag.CanBeTemporary];

        public override float modelPanelParametersMinDistance => 4f;
        public override float modelPanelParametersMaxDistance => 11f;
        public static BuffDef bdDiamondBoost;
        public static GameObject DiamondWardEffect;

        public override void Init()
        {
            base.Init();

            bdDiamondBoost = ScriptableObject.CreateInstance<BuffDef>();
            bdDiamondBoost.isHidden = false;
            bdDiamondBoost.isDebuff = false;
            bdDiamondBoost.canStack = true;
            bdDiamondBoost.isCooldown = false;
            bdDiamondBoost.buffColor = Color.white;
            bdDiamondBoost.iconSprite = Main.assets.LoadAsset<Sprite>("bdDiamondBoost.png");
            bdDiamondBoost.name = "Crown's Grace";
            ContentAddition.AddBuffDef(bdDiamondBoost);

            DiamondWardEffect = PrefabAPI.InstantiateClone(Paths.GameObject.CrippleWard, "DiamondWardEffect");
            DiamondWardEffect.RemoveComponent<Deployable>();
            DiamondWardEffect.RemoveComponent<EventFunctions>();
            DiamondWardEffect.RemoveComponent<StartEvent>();
            DiamondWardEffect.RemoveComponent<Rigidbody>();
            DiamondWardEffect.EditComponent<BuffWard>((x) => {
                x.buffDef = bdDiamondBoost;
                x.buffDuration = 1f;
                x.interval = 1f;
                x.invertTeamFilter = false;
            });

            MeshRenderer mesh = DiamondWardEffect.transform.Find("Totem").Find("Mesh").GetComponent<MeshRenderer>();
            mesh.sharedMaterials = new Material[] { 
                Paths.Material.matFireRingGemstone, Paths.Material.matBarrier
            };
            mesh.EditComponent<ObjectScaleCurve>((x) => {
                x.resetOnDisable = true;
                x.useOverallCurveOnly = true;
                x.overallCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.8f, 1.2f), new Keyframe(1f, 1f));
            });
            mesh.RemoveComponent<MeshCollider>();
            mesh.GetComponent<MeshFilter>().sharedMesh = ItemModel.GetComponentInChildren<MeshFilter>().sharedMesh;
            mesh.transform.parent.Find("Infection").gameObject.SetActive(false);
            mesh.transform.localPosition = Vector3.zero;
            mesh.transform.localRotation = Quaternion.Euler(270, 0, 0);
            mesh.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            mesh.transform.parent.gameObject.SetActive(false);
            mesh.RemoveComponent<DitherModel>();
            Light light = mesh.transform.parent.Find("Point Light").GetComponent<Light>();
            light.gameObject.SetActive(false);

            DiamondWardEffect.transform.Find("Decal").gameObject.SetActive(false);
            DiamondWardEffect.transform.Find("Indicator").Find("IndicatorSphere").GetComponent<MeshRenderer>().sharedMaterials = new Material[] {
                Paths.Material.matMajorConstructDefenseMatrixEdges, Paths.Material.matRunicLensMeteorStrikeImpactIndicator
            };

            DiamondWardEffect.transform.Find("Indicator").Find("IndicatorSphere").Find("Drips").gameObject.SetActive(false);
            DiamondWardEffect.transform.Find("Indicator").gameObject.SetActive(false);
        }

        public override void Hooks()
        {
            CharacterBody.onBodyInventoryChangedGlobal += CharacterBody_onBodyInventoryChangedGlobal;
            RecalculateStatsAPI.GetStatCoefficients += OnGetStatCoefficients;
        }

        private void OnGetStatCoefficients(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender)
            {
                var stack = sender.GetBuffCount(bdDiamondBoost);
                if (stack > 0)
                {
                    args.attackSpeedMultAdd += attackSpeedGain * stack;
                    args.damageMultAdd += damageGain * stack;
                }
            }
        }

        public override object GetItemStatsDef()
        {
            ItemStatsDef itemStatsDef = new();
            //
            itemStatsDef.descriptions.Add("Barrier:");
            itemStatsDef.valueTypes.Add(ItemStatsDef.ValueType.Healing);
            itemStatsDef.measurementUnits.Add(ItemStatsDef.MeasurementUnits.Percentage);
            //
            itemStatsDef.descriptions.Add("Attack Speed: ");
            itemStatsDef.valueTypes.Add(ItemStatsDef.ValueType.Damage);
            itemStatsDef.measurementUnits.Add(ItemStatsDef.MeasurementUnits.Percentage);
            //
            itemStatsDef.descriptions.Add("Damage: ");
            itemStatsDef.valueTypes.Add(ItemStatsDef.ValueType.Damage);
            itemStatsDef.measurementUnits.Add(ItemStatsDef.MeasurementUnits.Percentage);
            //
            itemStatsDef.calculateValuesNew = (luck, stack, procChance) =>
            {
                List<float> values = new()
                {
                    barrierGain,
                    attackSpeedGain * stack,
                    damageGain * stack
                };

                return values;
            };

            return itemStatsDef;
        }

        private void CharacterBody_onBodyInventoryChangedGlobal(CharacterBody body)
        {
            body.AddItemBehavior<CrownsDiamondController>(GetCount(body));
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
                    childName = "Head",
                    localPos = new Vector3(-0.01041F, 0.08162F, -0.00924F),
                    localAngles = new Vector3(85.0407F, 197.8464F, 22.78797F),
                    localScale = new Vector3(0.12683F, 0.11843F, 0.11843F),

                    followerPrefab = itemDisplay,
                    limbMask = LimbFlags.None,
                    followerPrefabAddress = new AssetReferenceGameObject("")
                }

            );

            #endregion

            return i;

        }
    }

    public class CrownsDiamondController : CharacterBody.ItemBehavior
    {
        public HealthComponent hc;
        public bool wasOutOfCombat = true;
        public BuffWard ward;
        public GameObject DiamondWard;
        public GameObject mesh;

        public void OnEnable()
        {
            body = GetComponent<CharacterBody>();
            hc = body.healthComponent;

            DiamondWard = GameObject.Instantiate(CrownsDiamond.DiamondWardEffect, body.transform);
            DiamondWard.GetComponent<TeamFilter>().teamIndex = body.teamComponent.teamIndex;
            ward = DiamondWard.GetComponent<BuffWard>();
            ward.onBuffApplied += OnBuffApplied;

            mesh = DiamondWard.transform.Find("Totem").gameObject;
            mesh.transform.localPosition = new Vector3(0, 0, 0.7f * body.bestFitRadius); 
        }

        private void OnBuffApplied(CharacterBody body)
        {
            for (int i = 0; i < stack - 1; i++) {
                body.AddTimedBuff(CrownsDiamond.bdDiamondBoost, 1f, int.MaxValue);
            }
        }

        public void OnDisable() {
            if (DiamondWard) {
                GameObject.Destroy(DiamondWard);
            }
        }

        public void FixedUpdate()
        {
            if (hc == null) {
                return;
            }

            if (wasOutOfCombat && !body.outOfCombat) {
                hc.AddBarrierAuthority(hc.fullCombinedHealth * CrownsDiamond.barrierGain);
            }

            if (hc.barrier > 0f) {
                ward.radius = 20f;
                ward.buffDef = CrownsDiamond.bdDiamondBoost;
                mesh.gameObject.SetActive(true);
                ward.rangeIndicator.gameObject.SetActive(true);
            }
            else {
                mesh.gameObject.SetActive(false);
                ward.radius = 0f;
                ward.buffDef = null;
                ward.rangeIndicator.gameObject.SetActive(false);
            }

            wasOutOfCombat = body.outOfCombat;
        }
    }
}