using Sandswept.Enemies.TinyImp;
using System.Linq;

namespace Sandswept.Interactables.Regular
{
    [ConfigSection("Interactables :: Imp Pot")]
    internal class ImpPot : InteractableBase<ImpPot>
    {
        public override string Name => "Imp Pot";

        public override DirectorAPI.InteractableCategory Category => InteractableCategory.Shrines;

        public override int MaxSpawnsPerStage => 1;

        public override int CreditCost => directorCreditCost;

        public override HullClassification Size => HullClassification.Golem;

        public override int MinimumStageToAppearOn => 1;

        public override int SpawnWeight => 1;

        public override bool SlightlyRandomizeOrientation => false;

        public override string inspectInfoDescription => $"When activated by a survivor, the pot releases a Tiny Imp for each nearby chest, lunar pod, void cradle, void potential and multishop terminal (up to {impCount}, multishops last). Each imp runs to its target, opens it for free and runs off with the item. Kill them before they get back to the pot to keep the loot.";

        public GameObject prefab;

        [ConfigField("Director Credit Cost", "", 20)]
        public static int directorCreditCost;

        [ConfigField("Imp Count", "Most imps (and so most chests raided) per pot.", 3)]
        public static int impCount;

        [ConfigField("Chest Search Radius", "", 80f)]
        public static float searchRadius;

        public override void Init()
        {
            if (TinyImp.Instance == null || !TinyImp.Instance.prefab || !TinyImp.Instance.prefabMaster)
            {
                // the pot needs the Tiny Imp enemy
                return;
            }

            base.Init();

            prefab = PrefabAPI.InstantiateClone(Paths.GameObject.ShrineCombat, "Imp Pot", true);

            var modelBase = prefab.transform.Find("Base");
            modelBase.Find("mdlShrineCombat").gameObject.SetActive(false);

            var mdl = Object.Instantiate(Main.assets.LoadAsset<GameObject>("mdlImpShrine.prefab"), modelBase);
            mdl.name = "mdlImpPot";
            var mesh = mdl.GetComponent<MeshFilter>().sharedMesh;
            // the model is rotated so mesh z points up; normalize to a fixed height since the import scale is unreliable
            var scale = 3.5f / mesh.bounds.size.z;
            mdl.transform.localScale = Vector3.one * scale;
            mdl.transform.localPosition = Vector3.zero;
            mdl.transform.localRotation = Quaternion.identity;
            // ModelLocator snaps the model onto Base every frame, so Base is what has to lift the pot out of the ground
            modelBase.localPosition = Vector3.up * (-mesh.bounds.min.z * scale);
            var collider = mdl.AddComponent<BoxCollider>();
            collider.center = mesh.bounds.center;
            collider.size = mesh.bounds.size;
            mdl.AddComponent<EntityLocator>().entity = prefab;

            prefab.GetComponent<ModelLocator>().modelTransform = mdl.transform;
            prefab.GetComponent<Highlight>().targetRenderer = mdl.GetComponent<MeshRenderer>();

            var symbol = prefab.transform.Find("Symbol");
            symbol.localPosition = new(0, 6f, 0);
            var symbolMaterial = new Material(symbol.GetComponent<MeshRenderer>().sharedMaterial);
            symbolMaterial.SetColor("_TintColor", new Color32(255, 40, 40, 255));
            symbol.GetComponent<MeshRenderer>().sharedMaterial = symbolMaterial;

            var purchaseInteraction = prefab.GetComponent<PurchaseInteraction>();
            purchaseInteraction.displayNameToken = "SANDSWEPT_IMP_POT_NAME";
            purchaseInteraction.contextToken = "SANDSWEPT_IMP_POT_CONTEXT";
            purchaseInteraction.Networkavailable = true;
            purchaseInteraction.costType = CostTypeIndex.None;
            purchaseInteraction.cost = 0;

            prefab.GetComponent<GenericDisplayNameProvider>().displayToken = "SANDSWEPT_IMP_POT_NAME";

            prefab.RemoveComponent<ShrineCombatBehavior>();
            prefab.RemoveComponent<CombatDirector>();
            prefab.AddComponent<ImpPotController>();

            interactableSpawnCard.prefab = prefab;

            PrefabAPI.RegisterNetworkPrefab(prefab);

            LanguageAPI.Add("SANDSWEPT_IMP_POT_NAME", "Imp Pot");
            LanguageAPI.Add("SANDSWEPT_IMP_POT_CONTEXT", "Disturb the Imp Pot");
            LanguageAPI.Add("SANDSWEPT_IMP_POT_USE_MESSAGE", "<style=cShrine>{0} disturbed the pot. The imps are going after your chests!</color>");
            LanguageAPI.Add("SANDSWEPT_IMP_POT_FAIL_MESSAGE", "<style=cShrine>The imps find nothing worth stealing nearby.</color>");

            SceneDirector.onPostPopulateSceneServer += ImpPotController.RemovePotsWithoutChests;

            PostInit();

            // Forgotten Relics' stage with the loose Relic of Energy; does nothing if that mod isn't installed
            Helpers.AddNewInteractableToStage(directorCardHolder, Stage.Custom, "slumberingsatellite");
        }
    }

    public class ImpPotController : ShrineBehavior
    {
        public PurchaseInteraction purchaseInteraction;
        public Transform symbolTransform;

        // targets an imp from this pot is already heading for, so two imps never go after the same chest
        public readonly HashSet<PurchaseInteraction> claimedTargets = new();

        public override int GetNetworkChannel()
        {
            return RoR2.Networking.QosChannelIndex.defaultReliable.intVal;
        }

        private void OnEnable()
        {
            InstanceTracker.Add(this);
        }

        private void OnDisable()
        {
            InstanceTracker.Remove(this);
        }

        private void Start()
        {
            purchaseInteraction = GetComponent<PurchaseInteraction>();
            symbolTransform = transform.Find("Symbol");

            if (NetworkServer.active)
            {
                purchaseInteraction.onPurchase.AddListener(OnPurchase);
            }
        }

        // a pot with nothing to steal is pointless, so pots placed away from every chest are removed once the stage is populated
        public static void RemovePotsWithoutChests(SceneDirector sceneDirector)
        {
            foreach (var pot in InstanceTracker.GetInstancesList<ImpPotController>().ToList())
            {
                // chests haven't rolled their items yet at this point, so only check that they're unopened
                if (!FindStealableChests(pot.transform.position, false).Any())
                {
                    NetworkServer.Destroy(pot.gameObject);
                }
            }
        }

        public const string RelicOfEnergyToken = "INTERACTIBLE_RELICENERGY_NAME";

        // chests, lunar pods, void cradles, void potentials, trishop terminals and Forgotten Relics' loose Relic of Energy
        public static IEnumerable<PurchaseInteraction> FindStealableChests(Vector3 origin, bool requireLoot)
        {
            var maxSqrDistance = ImpPot.searchRadius * ImpPot.searchRadius;
            return InstanceTracker.GetInstancesList<PurchaseInteraction>()
                .Where(x => x && x.available
                    && (x.transform.position - origin).sqrMagnitude <= maxSqrDistance
                    && CanSteal(x, requireLoot))
                // trishops go last, so the imps only raid one once the other nearby chests are bought; terminals of one shop stay together
                .OrderBy(x => x.TryGetComponent<ShopTerminalBehavior>(out _) ? 1 : 0)
                .ThenBy(x => (GroupPosition(x) - origin).sqrMagnitude)
                .ThenBy(x => (x.transform.position - origin).sqrMagnitude);
        }

        private static Vector3 GroupPosition(PurchaseInteraction purchaseInteraction)
        {
            return purchaseInteraction.TryGetComponent<ShopTerminalBehavior>(out var terminal) && terminal.serverMultiShopController
                ? terminal.serverMultiShopController.transform.position
                : purchaseInteraction.transform.position;
        }

        public static bool CanSteal(PurchaseInteraction purchaseInteraction, bool requireLoot)
        {
            var costType = purchaseInteraction.costType;
            var isChestCost = costType == CostTypeIndex.Money || costType == CostTypeIndex.PercentHealth || costType == CostTypeIndex.LunarCoin;

            if (purchaseInteraction.TryGetComponent<ChestBehavior>(out var chest))
            {
                return isChestCost && !chest.isChestOpened && (!requireLoot || chest.currentPickup.isValid);
            }

            if (purchaseInteraction.TryGetComponent<OptionChestBehavior>(out var optionChest))
            {
                return isChestCost && (!requireLoot || (optionChest.generatedPickups != null && optionChest.generatedPickups.Count > 0));
            }

            if (purchaseInteraction.TryGetComponent<ShopTerminalBehavior>(out var terminal))
            {
                var isDroneVendor = terminal.serverMultiShopController && terminal.serverMultiShopController.isTripleDroneVendor;
                return costType == CostTypeIndex.Money && !isDroneVendor && !terminal.hasBeenPurchased && (!requireLoot || terminal.CurrentPickup().isValid);
            }

            return purchaseInteraction.displayNameToken == RelicOfEnergyToken && GetRelicOfEnergyItem(purchaseInteraction);
        }

        // Forgotten Relics is a soft dependency, so its relic item is found through the relic's own drop component
        private static ItemDef GetRelicOfEnergyItem(PurchaseInteraction purchaseInteraction)
        {
            // GetComponent(string) goes through Unity's script registry, which can't be trusted to know another mod's scripts
            var dropper = purchaseInteraction.GetComponents<MonoBehaviour>().FirstOrDefault(x => x.GetType().Name == "CreatePickupDroplet");
            var provider = dropper ? dropper.GetType().Assembly.GetType("FRCSharp.VF2ContentPackProvider") : null;
            return provider?.GetField("relicOfVolatilityDef")?.GetValue(null) as ItemDef;
        }

        private void OnPurchase(Interactor interactor)
        {
            var chests = FindStealableChests(transform.position, true).Take(ImpPot.impCount).ToList();

            if (chests.Count == 0)
            {
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = "SANDSWEPT_IMP_POT_FAIL_MESSAGE" });
                purchaseInteraction.SetAvailable(true);
                return;
            }

            foreach (var chest in chests)
            {
                ReleaseImp(chest);
            }

            var interactorBody = interactor.GetComponent<CharacterBody>();
            if (interactorBody)
            {
                Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
                {
                    subjectAsCharacterBody = interactorBody,
                    baseToken = "SANDSWEPT_IMP_POT_USE_MESSAGE",
                });
            }

            EffectManager.SpawnEffect(Paths.GameObject.ImpBlinkEffect, new EffectData { origin = transform.position + Vector3.up * 2f, rotation = Quaternion.identity, scale = 2f }, true);
            Util.PlaySound("Play_imp_spawn", gameObject);

            purchaseInteraction.SetAvailable(false);
            symbolTransform.gameObject.SetActive(false);
            CallRpcSetPingable(false);
        }

        // called when an imp reaches its target: buys it for the player, but the imp takes what would have dropped
        public static GenericPickupController.CreatePickupInfo StealLoot(PurchaseInteraction purchaseInteraction, out Vector3 dropPosition)
        {
            purchaseInteraction.SetAvailable(false);

            if (purchaseInteraction.TryGetComponent<ChestBehavior>(out var chest))
            {
                var pickup = chest.currentPickup;
                chest.currentPickup = UniquePickup.none;
                chest.Open();
                chest.NetworkisChestOpened = true;

                dropPosition = chest.dropTransform ? chest.dropTransform.position : chest.transform.position;
                return new GenericPickupController.CreatePickupInfo
                {
                    pickup = pickup,
                    artifactFlag = chest.isCommandChest ? GenericPickupController.PickupArtifactFlag.Command : GenericPickupController.PickupArtifactFlag.None
                };
            }

            if (purchaseInteraction.TryGetComponent<ShopTerminalBehavior>(out var terminal))
            {
                var terminalPickup = terminal.CurrentPickup();
                terminal.SetNoPickup();
                terminal.SetHasBeenPurchased(true);

                dropPosition = terminal.dropTransform ? terminal.dropTransform.position : terminal.transform.position;
                return new GenericPickupController.CreatePickupInfo { pickup = terminalPickup };
            }

            if (purchaseInteraction.TryGetComponent<OptionChestBehavior>(out var optionChest))
            {
                // void potential: the same drop OptionChestBehavior.ItemDrop would make
                var info = new GenericPickupController.CreatePickupInfo
                {
                    pickerOptions = PickupPickerController.GenerateOptionsFromList(optionChest.generatedPickups),
                    prefabOverride = optionChest.pickupPrefab,
                    rotation = Quaternion.identity,
                    pickupIndex = PickupCatalog.FindPickupIndex(optionChest.displayTier)
                };
                optionChest.generatedPickups = null;
                optionChest.Open();

                dropPosition = optionChest.dropTransform ? optionChest.dropTransform.position : optionChest.transform.position;
                return info;
            }

            // loose Relic of Energy: the imp grabs the relic itself off the ground
            var relic = GetRelicOfEnergyItem(purchaseInteraction);
            dropPosition = purchaseInteraction.transform.position;
            NetworkServer.Destroy(purchaseInteraction.gameObject);
            // not a chest drop, so Command must not turn it into an essence
            return new GenericPickupController.CreatePickupInfo { pickup = new UniquePickup(PickupCatalog.FindPickupIndex(relic.itemIndex)) { ignoreArtifactBasedPickupReplacements = true } };
        }

        // for an imp whose target got bought or taken before it arrived
        public PurchaseInteraction ClaimNextTarget()
        {
            var target = FindStealableChests(transform.position, true).FirstOrDefault(x => !claimedTargets.Contains(x));
            if (target)
            {
                claimedTargets.Add(target);
            }

            return target;
        }

        private void ReleaseImp(PurchaseInteraction target)
        {
            var spawnPosition = transform.position + Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up) * Vector3.forward * 4f + Vector3.up;
            var impMaster = new MasterSummon
            {
                masterPrefab = TinyImp.Instance.prefabMaster,
                position = spawnPosition,
                rotation = Quaternion.identity,
                teamIndexOverride = TeamIndex.Monster,
                ignoreTeamMemberLimit = true,
                useAmbientLevel = true,
            }.Perform();

            var impBody = impMaster ? impMaster.GetBody() : null;
            if (!impBody)
            {
                return;
            }

            claimedTargets.Add(target);
            EffectManager.SpawnEffect(Paths.GameObject.ImpBlinkEffect, new EffectData { origin = impBody.corePosition, rotation = Quaternion.identity }, true);
            impBody.GetComponent<TinyImpCarrier>().Init(target, this);
        }
    }
}
