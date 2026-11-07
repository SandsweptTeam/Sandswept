using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2.Artifacts;
using RoR2.CharacterAI;
using RoR2.Navigation;
using System.Linq;
using Sandswept.Interactables.Regular;

namespace Sandswept.Enemies.TinyImp
{
    // runs from its imp pot to a chest, steals what's inside, then carries it back zigzagging and blinking; drops the item if killed on the way back
    public class TinyImpCarrier : MonoBehaviour, IOnKilledServerReceiver
    {
        public const float ReachDistance = 4f;
        public const float StealDistance = 5f;
        public const float StuckBlinkTime = 15f;
        public const float ZigzagInterval = 0.8f;
        public const float ZigzagWidth = 7f;
        public const float StepLength = 12f;

        // what drops if the imp dies: chest items (a whole Scavenger bag's worth at most), or a void potential (picker options)
        public List<GenericPickupController.CreatePickupInfo> loot = new();
        public bool hasLoot;
        public Transform pot;
        public ImpPotController potController;
        public MonoBehaviour target;
        // an Aetherium brazier flame the imp is carrying instead of an item, -1 if none
        public int brazierBuffIndex = -1;

        private CharacterBody body;
        private GameObject waypoint;
        private bool returning;
        private float outboundTimer;
        private float zigzagTimer;
        private float blinkTimer;
        private int zigzagSide = 1;
        private readonly List<GameObject> heldDisplays = new();

        public void Awake()
        {
            body = GetComponent<CharacterBody>();
        }

        public void Init(MonoBehaviour target, ImpPotController potController)
        {
            this.target = target;
            this.potController = potController;
            pot = potController.transform;

            waypoint = new GameObject("TinyImpWaypoint");
            waypoint.transform.position = target.transform.position;
            body.master.GetComponent<BaseAI>().SetCustomTargetGameObject(waypoint);
        }

        public void FixedUpdate()
        {
            if (!NetworkServer.active || !pot)
            {
                return;
            }

            if (returning)
            {
                ReturnToPot();
            }
            else
            {
                GoToTarget();
            }
        }

        private void GoToTarget()
        {
            // the target was bought, stolen or destroyed before we got there
            if (!target || !ImpPotController.CanSteal(target, true))
            {
                ReleaseTarget();
                target = potController ? potController.ClaimNextTarget() : null;
                outboundTimer = 0f;
                if (!target)
                {
                    // nothing left to steal, head home empty-handed
                    returning = true;
                    return;
                }
            }

            var targetPosition = target.transform.position;
            waypoint.transform.position = targetPosition;

            if ((targetPosition - body.footPosition).sqrMagnitude <= StealDistance * StealDistance)
            {
                Steal();
                return;
            }

            outboundTimer += Time.fixedDeltaTime;
            if (outboundTimer >= StuckBlinkTime)
            {
                // probably can't path there, so blink next to it instead of standing around
                outboundTimer = 0f;
                Blink(SnapToGround(targetPosition), targetPosition - body.footPosition);
            }
        }

        private void Steal()
        {
            // read first, emptying the terminal clears it; the real item stays hidden until it drops
            bool isHidden = target.TryGetComponent<ShopTerminalBehavior>(out var terminal) && terminal.pickupIndexIsHidden;
            brazierBuffIndex = ImpPotController.GetBrazierBuffIndex(target);
            loot = potController.StealLoot(target, body);
            hasLoot = true;
            ReleaseTarget();
            target = null;

            returning = true;
            blinkTimer = Random.Range(1.5f, 3f);
            Util.PlaySound("Play_imp_attack_blink", gameObject);

            var pickups = loot.Select(x => x.pickupIndex).ToArray();
            var commandEssences = loot.Select(WillDropCommandEssence).ToArray();
            bool isVoidPotential = loot.Any(x => x.prefabOverride);
            SetCarriedPickupDisplay(pickups, commandEssences, isVoidPotential, isHidden);
            new SetCarriedPickupMessage(gameObject, pickups, commandEssences, isVoidPotential, isHidden).Send(NetworkDestination.Clients);
        }

        // same check as PickupDropletController.CreatePickup, so the imp holds whatever would land if it died
        private static bool WillDropCommandEssence(GenericPickupController.CreatePickupInfo info)
        {
            bool isCommand = CommandArtifactManager.IsCommandArtifactEnabled || (info.artifactFlag & GenericPickupController.PickupArtifactFlag.Command) != 0;
            bool isDelusion = (info.artifactFlag & GenericPickupController.PickupArtifactFlag.Delusion) != 0;
            var pickupDef = PickupCatalog.GetPickupDef(info.pickupIndex);

            return isCommand && !isDelusion && !info.pickup.ignoreArtifactBasedPickupReplacements && pickupDef != null
                && (pickupDef.itemIndex != ItemIndex.None || pickupDef.equipmentIndex != EquipmentIndex.None || pickupDef.itemTier != ItemTier.NoTier);
        }

        private void ReleaseTarget()
        {
            if (target && potController)
            {
                potController.claimedTargets.Remove(target);
            }
        }

        private void ReturnToPot()
        {
            var toPot = pot.position - body.footPosition;
            toPot.y = 0f;
            var distance = toPot.magnitude;

            if (distance <= ReachDistance)
            {
                ReachPot();
                return;
            }

            var forward = toPot / distance;
            var right = Vector3.Cross(Vector3.up, forward);

            zigzagTimer -= Time.fixedDeltaTime;
            if (zigzagTimer <= 0f)
            {
                zigzagTimer = ZigzagInterval;
                zigzagSide = -zigzagSide;
                waypoint.transform.position = distance <= StepLength
                    ? pot.position
                    : SnapToGround(body.footPosition + forward * StepLength + right * (ZigzagWidth * zigzagSide));
            }

            if (!hasLoot)
            {
                return;
            }

            blinkTimer -= Time.fixedDeltaTime;
            if (blinkTimer <= 0f)
            {
                blinkTimer = Random.Range(2.5f, 4.5f);
                // no blinking straight into the pot
                if (distance > 15f)
                {
                    Blink(SnapToGround(body.footPosition + forward * Random.Range(4f, 10f) + right * Random.Range(-9f, 9f)), forward);
                }
            }
        }

        private void Blink(Vector3 destination, Vector3 forward)
        {
            var effectData = new EffectData { origin = body.corePosition, rotation = Util.QuaternionSafeLookRotation(forward) };
            EffectManager.SpawnEffect(Paths.GameObject.ImpBlinkEffect, effectData, true);
            Util.PlaySound("Play_imp_attack_blink", gameObject);

            TeleportHelper.TeleportBody(body, destination, false);

            effectData.origin = destination + Vector3.up;
            EffectManager.SpawnEffect(Paths.GameObject.ImpBlinkEffect, effectData, true);
        }

        private void ReachPot()
        {
            // the pot swallows the item, nothing drops
            hasLoot = false;
            EffectManager.SpawnEffect(Paths.GameObject.ImpBlinkEffect, new EffectData { origin = pot.position + Vector3.up * 2f, rotation = Quaternion.identity }, true);
            Util.PlaySound("Play_imp_attack_blink", pot.gameObject);

            var master = body.master;
            pot = null;
            if (master)
            {
                master.DestroyBody();
                NetworkServer.Destroy(master.gameObject);
            }
            else
            {
                NetworkServer.Destroy(gameObject);
            }
        }

        public void OnKilledServer(DamageReport damageReport)
        {
            // killed on the way out: the chest is untouched and free for someone else
            ReleaseTarget();
            target = null;

            if (!hasLoot)
            {
                return;
            }

            if (brazierBuffIndex >= 0)
            {
                // whoever killed the imp (or owns the minion that did) gets the flame
                var killerMaster = damageReport.attackerMaster;
                if (killerMaster && killerMaster.minionOwnership.ownerMaster)
                {
                    killerMaster = killerMaster.minionOwnership.ownerMaster;
                }
                var killer = killerMaster ? killerMaster.GetBody() : null;
                if (killer)
                {
                    ImpPotController.SendBrazierFlame(brazierBuffIndex, body.corePosition, killer.gameObject);
                }
                ImpPotController.RemoveBrazierFlames(body.master);
            }

            for (int i = 0; i < loot.Count; i++)
            {
                var info = loot[i];
                info.position = body.corePosition;
                // a bag's worth fans out in a ring instead of landing in one pile
                var spread = loot.Count > 1 ? Quaternion.AngleAxis(360f * i / loot.Count, Vector3.up) * Vector3.forward * 5f : Vector3.zero;
                PickupDropletController.CreatePickupDroplet(info, body.corePosition, Vector3.up * 20f + spread);
            }
            hasLoot = false;
            new SetCarriedPickupMessage(gameObject, Array.Empty<PickupIndex>(), Array.Empty<bool>(), false, false).Send(NetworkDestination.Clients);
        }

        public void OnDestroy()
        {
            ReleaseTarget();

            if (waypoint)
            {
                Destroy(waypoint);
            }
        }

        // holds the same uninteractable pickup as the Solus Extractor, so the item gets its tier outline and glow; void potentials and command essences clone their own orb instead
        public void SetCarriedPickupDisplay(PickupIndex[] pickups, bool[] commandEssences, bool isVoidPotential, bool isHidden)
        {
            var modelTransform = body.modelLocator ? body.modelLocator.modelTransform : null;
            if (!modelTransform)
            {
                return;
            }

            modelTransform.GetComponent<Animator>()?.SetBool("isCarrying", pickups.Length > 0);

            foreach (var held in heldDisplays)
            {
                Destroy(held);
            }
            heldDisplays.Clear();

            var pivot = modelTransform.GetComponent<ChildLocator>()?.FindChild("HoldingPivot");
            if (!pivot)
            {
                return;
            }

            // stacked on top of each other, so a raided Scavenger bag becomes a tower
            for (int i = 0; i < pickups.Length; i++)
            {
                heldDisplays.Add(CreateHeldDisplay(pivot, pivot.position + Vector3.up * (0.9f * i), pickups[i], commandEssences[i], isVoidPotential, isHidden));
            }
        }

        private static GameObject CreateHeldDisplay(Transform pivot, Vector3 position, PickupIndex pickupIndex, bool isCommandEssence, bool isVoidPotential, bool isHidden)
        {
            var orbPrefab = isCommandEssence ? Paths.GameObject.CommandCube : isVoidPotential ? Paths.GameObject.OptionPickup : null;
            var source = orbPrefab ? orbPrefab.GetComponentInChildren<PickupDisplay>(true).gameObject : Paths.GameObject.GenericPickupUninteractable;
            var heldDisplay = Instantiate(source, position, Quaternion.identity, pivot);
            // the pivot sits inside a scaled rig; undo that so the pickup is the same size as one on the ground
            heldDisplay.transform.localScale = Vector3.one / Mathf.Max(pivot.lossyScale.x, 0.0001f);
            foreach (var collider in heldDisplay.GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }

            PickupDisplay display;
            if (heldDisplay.TryGetComponent<GenericPickupController>(out var controller))
            {
                // this is an unspawned local visual, so disabling keeps it out of the interactable trackers (on a host it could otherwise be taken for real)
                controller.enabled = false;
                display = controller.pickupDisplay;
                // the prefab floats the model 1.5m above its glow; keep both at the imp's hands
                display.transform.localPosition = Vector3.zero;
            }
            else
            {
                // the clone still points at the source prefab's outline, so give it its own
                display = heldDisplay.GetComponent<PickupDisplay>();
                display.highlight = heldDisplay.AddComponent<Highlight>();
                display.highlight.highlightColor = Highlight.HighlightColor.pickup;
                // the essence bobs a full metre, which would pull it out of the imp's hands
                display.verticalWave.amplitude = 0f;
            }

            display.SetPickup(new UniquePickup(pickupIndex), isHidden);
            return heldDisplay;
        }

        private static Vector3 SnapToGround(Vector3 position)
        {
            var nodes = SceneInfo.instance ? SceneInfo.instance.groundNodes : null;
            if (!nodes)
            {
                return position;
            }

            var node = nodes.FindClosestNode(position, HullClassification.Human);
            return node != NodeGraph.NodeIndex.invalid && nodes.GetNodePosition(node, out var nodePosition) ? nodePosition : position;
        }

        public class SetCarriedPickupMessage : INetMessage
        {
            private GameObject imp;
            private PickupIndex[] pickups;
            private bool[] commandEssences;
            private bool isVoidPotential;
            private bool isHidden;

            public SetCarriedPickupMessage(GameObject imp, PickupIndex[] pickups, bool[] commandEssences, bool isVoidPotential, bool isHidden)
            {
                this.imp = imp;
                this.pickups = pickups;
                this.commandEssences = commandEssences;
                this.isVoidPotential = isVoidPotential;
                this.isHidden = isHidden;
            }

            public SetCarriedPickupMessage()
            { }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(imp);
                writer.Write(pickups.Length);
                for (int i = 0; i < pickups.Length; i++)
                {
                    writer.Write(pickups[i].value);
                    writer.Write(commandEssences[i]);
                }
                writer.Write(isVoidPotential);
                writer.Write(isHidden);
            }

            public void Deserialize(NetworkReader reader)
            {
                imp = reader.ReadGameObject();
                int count = reader.ReadInt32();
                pickups = new PickupIndex[count];
                commandEssences = new bool[count];
                for (int i = 0; i < count; i++)
                {
                    pickups[i] = new PickupIndex(reader.ReadInt32());
                    commandEssences[i] = reader.ReadBoolean();
                }
                isVoidPotential = reader.ReadBoolean();
                isHidden = reader.ReadBoolean();
            }

            public void OnReceived()
            {
                if (imp && imp.TryGetComponent<TinyImpCarrier>(out var carrier))
                {
                    carrier.SetCarriedPickupDisplay(pickups, commandEssences, isVoidPotential, isHidden);
                }
            }
        }
    }
}
