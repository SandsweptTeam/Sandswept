using MonoMod.RuntimeDetour;
using Sandswept.Enemies.TinyImp;
using System.Linq;
using System.Reflection;

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

        public override string inspectInfoDescription => $"When activated by a survivor, the pot releases a Tiny Imp for each nearby chest, lunar pod, void cradle, void potential, multishop terminal, Fuel Array, page and modded shrine (up to {impCount}, multishops last). Each imp runs to its target, opens it for free and runs off with the item. Kill them before they get back to the pot to keep the loot.";

        public GameObject prefab;

        [ConfigField("Director Credit Cost", "", 20)]
        public static int directorCreditCost;

        [ConfigField("Imp Count", "Most imps (and so most chests raided) per pot.", 3)]
        public static int impCount;

        [ConfigField("Chest Search Radius", "", 80f)]
        public static float searchRadius;

        [ConfigField("Troll Mode", "On their way to a target, imps set off nearby combat and mountain shrines, newt altars, Ethereal Saplings, Halcyon Shrines, the shrines of Sandswept, MoreMountains and Extra Challenge Shrines, and the teleporter. A stolen 57 Leaf Clover gets taken to a nearby scrapper first. Imps can also wake the Ancient Sepulcher and run off with N'tormat and a page.", false)]
        public static bool trollMode;

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

            // inherited from ShrineCombat; Drifter's Repossess (and anything else using it) would pick the pot up
            foreach (var attributes in prefab.GetComponentsInChildren<SpecialObjectAttributes>(true))
            {
                attributes.grabbable = false;
            }

            interactableSpawnCard.prefab = prefab;

            PrefabAPI.RegisterNetworkPrefab(prefab);

            LanguageAPI.Add("SANDSWEPT_IMP_POT_NAME", "Imp Pot");
            LanguageAPI.Add("SANDSWEPT_IMP_POT_CONTEXT", "Disturb the Imp Pot");
            LanguageAPI.Add("SANDSWEPT_IMP_POT_USE_MESSAGE", "<style=cShrine>{0} disturbed the pot. The imps are going after your chests!</color>");
            LanguageAPI.Add("SANDSWEPT_IMP_POT_FAIL_MESSAGE", "<style=cShrine>The imps find nothing worth stealing nearby.</color>");

            SceneDirector.onPostPopulateSceneServer += ImpPotController.RemovePotsWithoutChests;
            RoR2Application.onLoad += ImpPotController.HookLynxShrineName;

            PostInit();

            // Forgotten Relics' stage with the loose Relic of Energy; does nothing if that mod isn't installed
            Helpers.AddNewInteractableToStage(directorCardHolder, Stage.Custom, "slumberingsatellite");
            // Void Fields spawns chests and trishops, and SS2's void rock
            Helpers.AddNewInteractableToStage(directorCardHolder, Stage.VoidCell);
        }
    }

    public class ImpPotController : ShrineBehavior
    {
        public PurchaseInteraction purchaseInteraction;
        public Transform symbolTransform;

        // targets an imp from this pot is already heading for, so two imps never go after the same chest
        public readonly HashSet<MonoBehaviour> claimedTargets = new();

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
        public const string JudgementShrineToken = "OBJECT_BOSSSHRINE_NAME";
        public const float TrollDetourDistance = 40f;

        private static readonly HashSet<string> TrollShrines = new()
        {
            nameof(ShrineBossBehavior), nameof(ShrineCombatBehavior), nameof(ShrineCombatTroopBehavior), nameof(PortalStatueBehavior), nameof(HalcyoniteShrineInteractable),
            nameof(ShrineOfRuinController), nameof(ShrineOfSacrificeController), nameof(ShrineOfTheFutureController),
            "ShrineEtherealBehavior", "MoreMountainShrineBehavior",
            "ExtraChallengeShrinesShrineCrownBehaviour", "ExtraChallengeShrinesShrineEyeBehaviour", "ExtraChallengeShrinesShrineRockBehaviour",
        };

        private List<MonoBehaviour> trollTargets = new();

        // purchasables (chests, lunar pods, void cradles/potentials, trishops, modded shrines and pedestals), Fuel Arrays and the Ancient Sepulcher's N'tormat
        public static IEnumerable<MonoBehaviour> FindStealableChests(Vector3 origin, bool requireLoot)
        {
            var maxSqrDistance = ImpPot.searchRadius * ImpPot.searchRadius;
            return InstanceTracker.GetInstancesList<PurchaseInteraction>().Cast<MonoBehaviour>()
                // the pod's Fuel Array stays disabled until its panel is opened, so it isn't in any tracker
                .Concat(Object.FindObjectsOfType<GenericPickupController>())
                .Append(ImpPot.trollMode ? WakeController.instance : null)
                .Where(x => x
                    && (x.transform.position - origin).sqrMagnitude <= maxSqrDistance
                    && CanSteal(x, requireLoot))
                // trishops go last, so the imps only raid one once the other nearby chests are bought; terminals of one shop stay together
                .OrderBy(x => IsShopTerminal(x) ? 1 : 0)
                .ThenBy(x => (GroupPosition(x) - origin).sqrMagnitude)
                .ThenBy(x => (x.transform.position - origin).sqrMagnitude);
        }

        private static bool IsShopTerminal(MonoBehaviour target)
        {
            return target.TryGetComponent<ShopTerminalBehavior>(out var terminal) && terminal.serverMultiShopController;
        }

        private static Vector3 GroupPosition(MonoBehaviour target)
        {
            return target.TryGetComponent<ShopTerminalBehavior>(out var terminal) && terminal.serverMultiShopController
                ? terminal.serverMultiShopController.transform.position
                : target.transform.position;
        }

        // other mods are soft dependencies, so their components are found by type name
        private static MonoBehaviour FindModComponent(Component target, string typeName)
        {
            // GetComponent(string) goes through Unity's script registry, which can't be trusted to know another mod's scripts
            return target.GetComponents<MonoBehaviour>().FirstOrDefault(x => x && x.GetType().Name == typeName);
        }

        private static object GetMember(object instance, string name)
        {
            var type = instance.GetType();
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            return type.GetField(name, flags)?.GetValue(instance) ?? type.GetProperty(name, flags)?.GetValue(instance);
        }

        public static bool CanSteal(MonoBehaviour target, bool requireLoot)
        {
            if (target is WakeController wake)
            {
                // the Ancient Sepulcher, until someone (or an imp) has woken it
                return wake.missionStatus == WakeController.MissionStatus.Idle && !wake.hasDroppedMainReward && wake.GetInteractability(null) == Interactability.Available;
            }

            if (target is GenericPickupController pickupController)
            {
                if (pickupController.consumed)
                {
                    return false;
                }

                // PagePotentialHidden only rolls its page in Start, so it can still be empty while the stage populates
                if (pickupController.GetComponent<PagePotentialPicker>())
                {
                    return !requireLoot || pickupController.pickup.isValid;
                }

                var pickupIndex = pickupController.pickup.pickupIndex;
                return pickupIndex == PickupCatalog.FindPickupIndex(RoR2Content.Equipment.QuestVolatileBattery.equipmentIndex) || pickupIndex == PickupCatalog.FindPickupIndex(DLC4Content.KeyItems.CursedBook.keyItemIndex);
            }

            if (target is not PurchaseInteraction purchaseInteraction || !purchaseInteraction.available)
            {
                return false;
            }

            var costType = purchaseInteraction.costType;

            // any cost goes: Rusty Lockboxes and Encrusted Caches take keys, the Concept Reliquary takes items
            if (purchaseInteraction.TryGetComponent<ChestBehavior>(out var chest))
            {
                return !chest.isChestOpened && (!requireLoot || chest.currentPickup.isValid);
            }

            if (purchaseInteraction.TryGetComponent<OptionChestBehavior>(out var optionChest))
            {
                return !requireLoot || (optionChest.generatedPickups != null && optionChest.generatedPickups.Count > 0);
            }

            if (purchaseInteraction.TryGetComponent<ShopTerminalBehavior>(out var terminal))
            {
                var isDroneVendor = terminal.serverMultiShopController && terminal.serverMultiShopController.isTripleDroneVendor;
                // free terminals are Shipping Request Form deliveries
                var isShop = (costType == CostTypeIndex.Money || costType == CostTypeIndex.None) && !isDroneVendor;
                // Sivs' Shrine of Judgement and Starstorm 2's Remuneration pedestals are terminals too
                var isModdedTerminal = purchaseInteraction.displayNameToken == JudgementShrineToken || FindModComponent(purchaseInteraction, "RemunerationChoiceBehavior");
                return (isShop || isModdedTerminal) && !terminal.hasBeenPurchased && (!requireLoot || terminal.CurrentPickup().isValid);
            }

            var lynxShrine = FindModComponent(purchaseInteraction, "LynxTribeShrine");
            if (lynxShrine)
            {
                return !(bool)GetMember(lynxShrine, "activated") && ((PickupIndex)GetMember(lynxShrine, "pickupIndex")).isValid;
            }

            var brazier = FindModComponent(purchaseInteraction, "BuffBrazierManager");
            if (brazier)
            {
                return (int)GetMember(brazier, "ChosenBuffIndex") >= 0;
            }

            if (GetVoidRockItem(purchaseInteraction))
            {
                return true;
            }

            return purchaseInteraction.displayNameToken == RelicOfEnergyToken && GetRelicOfEnergyItem(purchaseInteraction);
        }

        // EnemiesReturns' GetDisplayName reads the shrine's item without a null check, so anything naming an emptied shrine (like a ping) throws
        public static void HookLynxShrineName()
        {
            var shrine = AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetType("EnemiesReturns.Enemies.LynxTribe.LynxTribeShrine")).FirstOrDefault(x => x != null);
            if (shrine != null)
            {
                new Hook(shrine.GetMethod("GetDisplayName"), typeof(ImpPotController).GetMethod(nameof(LynxShrineName), BindingFlags.NonPublic | BindingFlags.Static));
            }
        }

        private static string LynxShrineName(Func<MonoBehaviour, string> orig, MonoBehaviour self)
        {
            return ((PickupIndex)GetMember(self, "pickupIndex")).isValid ? orig(self) : Language.GetString("ENEMIES_RETURNS_LYNX_SHRINE_NAME");
        }

        private static bool IsTrollShrine(MonoBehaviour target)
        {
            return target.GetComponents<MonoBehaviour>().Any(x => x && TrollShrines.Contains(x.GetType().Name) && (x is not PortalStatueBehavior statue || statue.portalType == PortalStatueBehavior.PortalType.Shop));
        }

        private bool CanTroll(MonoBehaviour target)
        {
            var activator = purchaseInteraction.lastActivator;
            if (!activator)
            {
                return false;
            }

            // only ever started, interacting with a charged teleporter would end the stage
            return target is TeleporterInteraction teleporter
                ? teleporter.isIdle && teleporter.GetInteractability(activator) == Interactability.Available
                : ((PurchaseInteraction)target).available;
        }

        public MonoBehaviour ClaimTrollTarget(Vector3 position)
        {
            var target = trollTargets
                .Where(x => x && !claimedTargets.Contains(x) && (x.transform.position - position).sqrMagnitude <= TrollDetourDistance * TrollDetourDistance)
                .OrderBy(x => (x.transform.position - position).sqrMagnitude)
                .FirstOrDefault(CanTroll);

            if (target)
            {
                claimedTargets.Add(target);
            }

            return target;
        }

        public bool IsTrollValid(MonoBehaviour target)
        {
            return target && CanTroll(target);
        }

        // same filters as Shrine of the Wind's own picker
        private static BodyIndex FindMostExpensiveBoss()
        {
            var director = TeleporterInteraction.instance ? TeleporterInteraction.instance.bossDirector : null;
            var selection = director ? director.finalMonsterCardsSelection : null;
            var boss = BodyIndex.None;
            var highestCost = -1;

            for (int i = 0; selection != null && i < selection.Count; i++)
            {
                var card = selection.GetChoice(i).value;
                var body = card.spawnCard.prefab.GetComponent<CharacterMaster>().bodyPrefab.GetComponent<CharacterBody>();
                var forbidden = card.spawnCard is CharacterSpawnCard characterCard && characterCard.forbiddenAsBoss;
                if (body.isChampion && !forbidden && card.IsAvailable() && card.spawnCard.directorCreditCost > highestCost)
                {
                    highestCost = card.spawnCard.directorCreditCost;
                    boss = body.bodyIndex;
                }
            }

            return boss;
        }

        // sets the target off in the name of whoever disturbed the pot, free of charge
        public void Troll(MonoBehaviour target)
        {
            trollTargets.Remove(target);
            claimedTargets.Remove(target);
            var activator = purchaseInteraction.lastActivator;

            if (target is TeleporterInteraction teleporter)
            {
                teleporter.OnInteractionBegin(activator);
                return;
            }

            var interaction = (PurchaseInteraction)target;

            // Shrine of the Wind would open a boss picker for the activator, so the imp makes the choice for them
            var windShrine = FindModComponent(interaction, "ExtraChallengeShrinesShrineEyeBehaviour");
            var boss = windShrine ? FindMostExpensiveBoss() : BodyIndex.None;
            if (boss != BodyIndex.None)
            {
                interaction.SetAvailable(false);
                windShrine.GetType().GetField("selectedBody").SetValue(windShrine, boss);
                windShrine.GetType().GetMethod("AddShrineStack").Invoke(windShrine, new object[] { activator });
                return;
            }

            var costType = interaction.costType;
            var cost = interaction.cost;
            interaction.costType = CostTypeIndex.None;
            interaction.cost = 0;
            interaction.OnInteractionBegin(activator);
            interaction.costType = costType;
            interaction.cost = cost;
        }

        // Forgotten Relics is a soft dependency, so its relic item is found through the relic's own drop component
        private static ItemDef GetRelicOfEnergyItem(PurchaseInteraction purchaseInteraction)
        {
            var dropper = FindModComponent(purchaseInteraction, "CreatePickupDroplet");
            var provider = dropper ? dropper.GetType().Assembly.GetType("FRCSharp.VF2ContentPackProvider") : null;
            return provider?.GetField("relicOfVolatilityDef")?.GetValue(null) as ItemDef;
        }

        // Starstorm 2's void rock in the Void Fields, which drops Condemned Bond
        private static ItemDef GetVoidRockItem(PurchaseInteraction purchaseInteraction)
        {
            var rock = FindModComponent(purchaseInteraction, "VoidRock");
            var items = rock ? rock.GetType().Assembly.GetType("SS2.SS2Content+Items") : null;
            return items?.GetField("VoidRock")?.GetValue(null) as ItemDef;
        }

        // Aetherium's Buff Brazier: the imp takes the flame instead of an item, -1 if the target isn't a brazier
        public static int GetBrazierBuffIndex(MonoBehaviour target)
        {
            var brazier = target is PurchaseInteraction ? FindModComponent(target, "BuffBrazierManager") : null;
            return brazier ? (int)GetMember(brazier, "ChosenBuffIndex") : -1;
        }

        // sends a brazier flame flying to target, the same way buying the brazier does
        public static void SendBrazierFlame(int buffIndex, Vector3 origin, GameObject target)
        {
            var orbType = AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetType("Aetherium.Effect.BuffBrazierFlameOrb")).FirstOrDefault(x => x != null);
            if (orbType == null || !target)
            {
                return;
            }

            var orb = (RoR2.Orbs.Orb)Activator.CreateInstance(orbType);
            orbType.GetField("ChosenBuffIndex").SetValue(orb, buffIndex);
            orbType.GetField("Target").SetValue(orb, target);
            orb.origin = origin;
            RoR2.Orbs.OrbManager.instance.AddOrb(orb);
        }

        // removes the brazier flames orbiting a body, their controller cleans them up when destroyed
        public static void RemoveBrazierFlames(CharacterMaster master)
        {
            var flames = master ? FindModComponent(master, "BuffBrazierFlameOrbController") : null;
            if (flames)
            {
                Destroy(flames);
            }
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

            if (ImpPot.trollMode)
            {
                trollTargets = InstanceTracker.GetInstancesList<PurchaseInteraction>().Where(IsTrollShrine).Cast<MonoBehaviour>().Append(TeleporterInteraction.instance).Where(x => x).ToList();
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
        public List<GenericPickupController.CreatePickupInfo> StealLoot(MonoBehaviour target, CharacterBody thief)
        {
            // Scavenger bags (Twisted Scavenger ones too) roll a fresh drop each time, so the imp takes the whole bag's worth
            if (target.TryGetComponent<ChestBehavior>(out var bag) && bag.openState.stateType == typeof(EntityStates.ScavBackpack.Opening))
            {
                ((PurchaseInteraction)target).SetAvailable(false);
                var bagLoot = new List<GenericPickupController.CreatePickupInfo>();
                for (int i = 0; i < Mathf.Max(1, EntityStates.ScavBackpack.Opening.maxItemDropCount); i++)
                {
                    bag.RollItem();
                    if (bag.currentPickup.isValid)
                    {
                        bagLoot.Add(new GenericPickupController.CreatePickupInfo { pickup = bag.currentPickup });
                    }
                }
                bag.currentPickup = UniquePickup.none;
                bag.NetworkisChestOpened = true;
                bag.GetComponent<EntityStateMachine>().SetNextState(new EntityStates.Barrel.Opened());
                Util.PlaySound("Play_scav_backpack_open", bag.gameObject);
                return bagLoot;
            }

            var loot = StealOne(target, thief);
            var stolen = new List<GenericPickupController.CreatePickupInfo>();
            if (loot.pickupIndex.isValid)
            {
                stolen.Add(loot);
            }

            if (target is WakeController)
            {
                // the Sepulcher hands out a page alongside N'tormat
                stolen.Add(new GenericPickupController.CreatePickupInfo { pickup = DLC4ProgressionController.pageDropTable.GeneratePickup(Run.instance.treasureRng) });
            }

            return stolen;
        }

        private GenericPickupController.CreatePickupInfo StealOne(MonoBehaviour target, CharacterBody thief)
        {
            if (target is WakeController wake)
            {
                // wakes the Sepulcher the way interacting with it does, minus dropping the book; the imp has that now
                Util.PlaySound(wake.interactionSoundString, wake.gameObject);
                wake.RegisterParticipants();
                wake.hasDroppedMainReward = true;
                wake.UpdateMainRewardDisplay();
                wake.SetDirtyBit(8u);
                wake.missionEntityStateMachine.SetNextState(new EntityStates.Wake.SpawningSacrifices());
                return new GenericPickupController.CreatePickupInfo { pickup = wake.mainReward };
            }

            if (target is GenericPickupController fuelArray)
            {
                var fuelArrayPickup = fuelArray.pickup;
                NetworkServer.Destroy(fuelArray.gameObject);
                // never a chest drop, so Command must not turn it into an essence
                fuelArrayPickup.ignoreArtifactBasedPickupReplacements = true;
                return new GenericPickupController.CreatePickupInfo { pickup = fuelArrayPickup };
            }

            var purchaseInteraction = (PurchaseInteraction)target;
            purchaseInteraction.SetAvailable(false);

            if (purchaseInteraction.TryGetComponent<ChestBehavior>(out var chest))
            {
                var pickup = chest.currentPickup;
                chest.currentPickup = UniquePickup.none;
                if (chest.openState.stateType == typeof(EntityStates.HollowChest.Opening))
                {
                    // the Concept Reliquary's opening drains nearby survivors first; the imp just pops it open
                    chest.GetComponent<EntityStateMachine>().SetNextState(new EntityStates.Barrel.Opening());
                }
                else
                {
                    chest.Open();
                }
                chest.NetworkisChestOpened = true;

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

                if (purchaseInteraction.displayNameToken == JudgementShrineToken)
                {
                    // the shrine still starts its boss fight, it just has nothing to hand out at the end
                    foreach (var delayedEvent in purchaseInteraction.GetComponents<RoR2.EntityLogic.DelayedEvent>())
                    {
                        for (int i = 0; i < delayedEvent.action.GetPersistentEventCount(); i++)
                        {
                            if (delayedEvent.action.GetPersistentMethodName(i) == nameof(ShopTerminalBehavior.DropPickup))
                            {
                                delayedEvent.action.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
                            }
                        }
                    }
                    purchaseInteraction.onPurchase.Invoke(this.purchaseInteraction.lastActivator);
                }

                var choice = FindModComponent(purchaseInteraction, "RemunerationChoiceBehavior");
                var shop = choice ? GetMember(choice, "shop") : null;
                // taking one Remuneration pedestal's item takes the choice away, like picking it would
                shop?.GetType().GetMethod("OnChoicePicked")?.Invoke(shop, new object[] { choice });

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

                return info;
            }

            var lynxShrine = FindModComponent(purchaseInteraction, "LynxTribeShrine");
            if (lynxShrine)
            {
                // the tribe still turns up to fight; their shrine is left empty, so beating them drops nothing extra
                var lynxPickup = (PickupIndex)GetMember(lynxShrine, "pickupIndex");
                // not AddShrineStack: its escape timer reads the item and throws once the item is gone
                var spawner = GetMember(lynxShrine, "spawner");
                spawner?.GetType().GetMethod("SpawnLynxTribesmen")?.Invoke(spawner, new object[] { lynxShrine.transform });
                if (GetMember(lynxShrine, "shrineUseEffect") is GameObject useEffect && useEffect)
                {
                    EffectManager.SpawnEffect(useEffect, new EffectData { origin = lynxShrine.transform.position, rotation = Quaternion.identity, scale = 1f, color = Color.yellow }, true);
                }
                lynxShrine.GetType().GetMethod("DisableItemDisplay", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(lynxShrine, null);
                lynxShrine.GetType().GetProperty("NetworkpickupValue")?.SetValue(lynxShrine, PickupIndex.none.value);
                return new GenericPickupController.CreatePickupInfo { pickup = new UniquePickup(lynxPickup) };
            }

            var brazierBuffIndex = GetBrazierBuffIndex(purchaseInteraction);
            if (brazierBuffIndex >= 0)
            {
                // the flame orbits the imp until it's killed or makes it home
                var brazierStates = EntityStateMachine.FindByCustomName(purchaseInteraction.gameObject, "Body");
                var purchasedState = AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetType("Aetherium.States.Interactable.BuffBrazier.BuffBrazierPurchased")).FirstOrDefault(x => x != null);
                if (brazierStates && purchasedState != null)
                {
                    brazierStates.SetNextState((EntityState)Activator.CreateInstance(purchasedState));
                }

                var fireIcon = purchaseInteraction.transform.Find("Fire Icon");
                SendBrazierFlame(brazierBuffIndex, fireIcon ? fireIcon.position : purchaseInteraction.transform.position, thief.gameObject);
                return default;
            }

            var voidRockItem = GetVoidRockItem(purchaseInteraction);
            if (voidRockItem)
            {
                Util.PlaySound("Play_item_void_treasureCache_open", purchaseInteraction.gameObject);
                return new GenericPickupController.CreatePickupInfo { pickup = new UniquePickup(PickupCatalog.FindPickupIndex(voidRockItem.itemIndex)) };
            }

            // loose Relic of Energy: the imp grabs the relic itself off the ground
            var relic = GetRelicOfEnergyItem(purchaseInteraction);
            NetworkServer.Destroy(purchaseInteraction.gameObject);
            // not a chest drop, so Command must not turn it into an essence
            return new GenericPickupController.CreatePickupInfo { pickup = new UniquePickup(PickupCatalog.FindPickupIndex(relic.itemIndex)) { ignoreArtifactBasedPickupReplacements = true } };
        }

        // for an imp whose target got bought or taken before it arrived
        public MonoBehaviour ClaimNextTarget()
        {
            var target = FindStealableChests(transform.position, true).FirstOrDefault(x => !claimedTargets.Contains(x));
            if (target)
            {
                claimedTargets.Add(target);
            }

            return target;
        }

        private void ReleaseImp(MonoBehaviour target)
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
