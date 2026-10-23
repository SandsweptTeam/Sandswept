using System;
using System.Diagnostics;
using R2API.Networking;
using R2API.Networking.Interfaces;

namespace Sandswept.Utils {
    public class DamageTrailManager : MonoBehaviour {
        public static List<DamageTrail> activeDamageTrails = new();
        public static List<CharacterBody> bodies => CharacterBody.instancesList;
        public static DamageTrailManager instance;

        // networked trails are created and extended by the server only, clients get a copy of every segment so everyone
        // sees (and gets hurt by) the same trail. effects have to be registered on load so the index matches on every peer
        public static List<GameObject> trailEffects = new();
        public static Dictionary<int, DamageTrail> networkedTrails = new();
        private static int nextNetworkId = 1;

        public static void RegisterTrailEffect(GameObject effect) {
            if (!trailEffects.Contains(effect)) {
                trailEffects.Add(effect);
            }
        }

        public static DamageTrail DeployNetworkedDamageTrail(CharacterBody owner, float damage, GameObject effect, float radius, float tickRate = 5f, float mergeRadius = 0f, float minimumMergeTime = 0f) {
            DamageTrail trail = DeployDamageTrail(owner, damage, effect, radius, tickRate);
            trail.mergeRadius = mergeRadius;
            trail.minimumMergeTime = minimumMergeTime;

            int effectIndex = trailEffects.IndexOf(effect);
            if (effectIndex < 0) {
                Main.ModLogger.LogError($"DamageTrailManager: trail effect {effect} isn't registered, trail won't be networked");
                return trail;
            }

            trail.networkId = nextNetworkId++;
            new DamageTrailSync(trail, effectIndex).Send(NetworkDestination.Clients);

            return trail;
        }

        public static void AddSegmentNetworked(DamageTrail trail, Vector3 position, float lifetime, bool resetAllLifetime = false) {
            AddSegment(trail, position, lifetime, resetAllLifetime);

            if (trail.networkId != 0) {
                new DamageTrailSync(trail.networkId, position, lifetime, resetAllLifetime).Send(NetworkDestination.Clients);
            }
        }

        public static void DestroyTrailNetworked(DamageTrail trail) {
            DestroyTrail(trail);

            if (trail.networkId != 0) {
                new DamageTrailSync(trail.networkId).Send(NetworkDestination.Clients);
            }
        }

        public static DamageTrail DeployDamageTrail(CharacterBody owner, float damage, GameObject effect, float radius, float tickRate = 5f, DamageTypeCombo damageType = default) {
            DamageTrail trail = new();
            trail.owner = owner;
            trail.damage = owner.damage * damage;
            trail.delay = 1f / tickRate;
            trail.pointEffect = effect;
            trail.damageType = damageType;
            trail.activePoints = new();
            trail.radius = radius;
            trail.bounds = new Bounds(Vector3.zero, Vector3.zero);
            trail.team = owner.teamComponent.teamIndex;

            activeDamageTrails.Add(trail);

            return trail;
        }

        public static void AddSegment(DamageTrail trail, Vector3 position, float lifetime, bool resetAllLifetime = false) {
            if (trail.mergeRadius != 0f) {
                foreach (DamageTrailPoint point in trail.activePoints) {
                    if (Vector3.Distance(point.location, position) <= trail.mergeRadius && point.totalLifetime >= trail.minimumMergeTime) {
                        point.lifetime = lifetime;
                        return;
                    }
                }
            }

            if (resetAllLifetime) {
                foreach (DamageTrailPoint point in trail.activePoints) {
                    point.lifetime = lifetime;
                }
            }

            GameObject effect = GameObject.Instantiate(trail.pointEffect, position, Quaternion.identity);
            effect.transform.localScale = Vector3.one * trail.radius;
            trail.activePoints.Add(new DamageTrailPoint() {
                location = position,
                lifetime = lifetime,
                effectInstance = effect
            });

            RecalculateTrailBounds(trail);
        }

        public static void DestroyTrailImmediate(DamageTrail trail) {
            foreach (DamageTrailPoint point in trail.activePoints) {
                if (point.effectInstance) {
                    GameObject.Destroy(point.effectInstance);
                }
            }

            trail.activePoints.Clear();
            DestroyTrail(trail);
        }

        public static void DestroyTrail(DamageTrail trail) {
            trail.destroyed = true;
        }

        public void Start() {
            instance = this;

            // these are static, don't carry trails over from a previous run
            activeDamageTrails.Clear();
            networkedTrails.Clear();
        }

        public void FixedUpdate() {
            foreach (DamageTrail trail in activeDamageTrails) {
                trail.stopwatch += Time.fixedDeltaTime;

                if (trail.stopwatch >= trail.delay) {
                    trail.stopwatch = 0f;

                    // use the owner's current team so allies (or anything that changed team after the trail was deployed) don't hurt their own side
                    TeamIndex team = trail.owner && trail.owner.teamComponent ? trail.owner.teamComponent.teamIndex : trail.team;

                    foreach (CharacterBody body in bodies) {
                        if (!body.hasAuthority) continue;
                        if (body == trail.owner || !body.healthComponent || !body.teamComponent) continue;
                        if (!FriendlyFireManager.ShouldSplashHitProceed(body.healthComponent, team)) continue;
                        if (!trail.bounds.Contains(body.footPosition)) continue; // skip calc if we know for certain we are well beyond range of any node

                        foreach (DamageTrailPoint point in trail.activePoints) {
                            if (Vector3.Distance(body.footPosition, point.location) <= trail.radius) {
                                DamageInfo info = trail.GetDamageInfo(body);
                                if (NetworkServer.active) {
                                    body.healthComponent.TakeDamage(info);
                                    GlobalEventManager.instance.OnHitAll(info, body.gameObject);
                                    GlobalEventManager.instance.OnHitEnemy(info, body.gameObject);
                                }
                                else {
                                    body.healthComponent.RequestTakeDamage(info);
                                }
                                
                                break;
                            }
                        }
                    }
                }

                foreach (DamageTrailPoint point in trail.activePoints) {
                    point.lifetime -= Time.fixedDeltaTime;
                    point.totalLifetime += Time.fixedDeltaTime;

                    if (point.lifetime <= 0f && point.effectInstance) {
                        GameObject.Destroy(point.effectInstance);
                    }
                }

                int count = trail.activePoints.Count;
                trail.activePoints.RemoveAll(x => x.lifetime <= 0f);
                if (count != trail.activePoints.Count) {
                    RecalculateTrailBounds(trail);
                }
            }
            
            activeDamageTrails.RemoveAll(x => (x.owner == null || x.destroyed) && x.activePoints.Count <= 0);
        }

        public class DamageTrailSync : INetMessage {
            public enum Operation : byte {
                Deploy,
                AddSegment,
                Destroy
            }

            public Operation operation;
            public int networkId;

            // deploy
            public GameObject owner;
            public int effectIndex;
            public float damage;
            public float delay;
            public float radius;
            public float mergeRadius;
            public float minimumMergeTime;
            public DamageTypeCombo damageType;

            // segment
            public Vector3 position;
            public float lifetime;
            public bool resetAllLifetime;

            public DamageTrailSync() {
            }

            public DamageTrailSync(DamageTrail trail, int effectIndex) {
                operation = Operation.Deploy;
                networkId = trail.networkId;
                owner = trail.owner ? trail.owner.gameObject : null;
                this.effectIndex = effectIndex;
                damage = trail.damage;
                delay = trail.delay;
                radius = trail.radius;
                mergeRadius = trail.mergeRadius;
                minimumMergeTime = trail.minimumMergeTime;
                damageType = trail.damageType;
            }

            public DamageTrailSync(int networkId, Vector3 position, float lifetime, bool resetAllLifetime) {
                operation = Operation.AddSegment;
                this.networkId = networkId;
                this.position = position;
                this.lifetime = lifetime;
                this.resetAllLifetime = resetAllLifetime;
            }

            public DamageTrailSync(int networkId) {
                operation = Operation.Destroy;
                this.networkId = networkId;
            }

            public void Serialize(NetworkWriter writer) {
                writer.Write((byte)operation);
                writer.Write(networkId);

                switch (operation) {
                    case Operation.Deploy:
                        writer.Write(owner);
                        writer.Write(effectIndex);
                        writer.Write(damage);
                        writer.Write(delay);
                        writer.Write(radius);
                        writer.Write(mergeRadius);
                        writer.Write(minimumMergeTime);
                        writer.WriteDamageType(damageType);
                        break;

                    case Operation.AddSegment:
                        writer.Write(position);
                        writer.Write(lifetime);
                        writer.Write(resetAllLifetime);
                        break;
                }
            }

            public void Deserialize(NetworkReader reader) {
                operation = (Operation)reader.ReadByte();
                networkId = reader.ReadInt32();

                switch (operation) {
                    case Operation.Deploy:
                        owner = reader.ReadGameObject();
                        effectIndex = reader.ReadInt32();
                        damage = reader.ReadSingle();
                        delay = reader.ReadSingle();
                        radius = reader.ReadSingle();
                        mergeRadius = reader.ReadSingle();
                        minimumMergeTime = reader.ReadSingle();
                        damageType = reader.ReadDamageType();
                        break;

                    case Operation.AddSegment:
                        position = reader.ReadVector3();
                        lifetime = reader.ReadSingle();
                        resetAllLifetime = reader.ReadBoolean();
                        break;
                }
            }

            public void OnReceived() {
                // the host already has the real trail
                if (NetworkServer.active) {
                    return;
                }

                switch (operation) {
                    case Operation.Deploy:
                        if (effectIndex < 0 || effectIndex >= trailEffects.Count) {
                            return;
                        }

                        DamageTrail trail = new() {
                            owner = owner ? owner.GetComponent<CharacterBody>() : null,
                            damage = damage,
                            delay = delay,
                            pointEffect = trailEffects[effectIndex],
                            damageType = damageType,
                            activePoints = new(),
                            radius = radius,
                            bounds = new Bounds(Vector3.zero, Vector3.zero),
                            mergeRadius = mergeRadius,
                            minimumMergeTime = minimumMergeTime,
                            networkId = networkId
                        };
                        trail.team = trail.owner && trail.owner.teamComponent ? trail.owner.teamComponent.teamIndex : TeamIndex.Monster;

                        activeDamageTrails.Add(trail);
                        networkedTrails[networkId] = trail;
                        break;

                    case Operation.AddSegment:
                        if (networkedTrails.TryGetValue(networkId, out DamageTrail segmentTrail)) {
                            AddSegment(segmentTrail, position, lifetime, resetAllLifetime);
                        }
                        break;

                    case Operation.Destroy:
                        if (networkedTrails.TryGetValue(networkId, out DamageTrail destroyedTrail)) {
                            DestroyTrail(destroyedTrail);
                            networkedTrails.Remove(networkId);
                        }
                        break;
                }
            }
        }

        public static void RecalculateTrailBounds(DamageTrail trail) {
            Vector3[] points = new Vector3[trail.activePoints.Count];
            for (int i = 0; i < points.Length; i++) {
                points[i] = trail.activePoints[i].location;
            }

            trail.bounds = GeometryUtility.CalculateBounds(points, instance.transform.localToWorldMatrix);
            trail.bounds.size += Vector3.one * trail.radius;
        }
    }

    public class DamageTrail {
        public CharacterBody owner;
        public TeamIndex team;
        public float delay;
        public float damage;
        public GameObject pointEffect;
        public DamageTypeCombo damageType;
        public float radius;
        public Bounds bounds;
        public List<DamageTrailPoint> activePoints;
        public float stopwatch;
        public bool destroyed = false;
        public float mergeRadius = 0f;
        public float minimumMergeTime = 0f;
        public int networkId = 0;

        public DamageInfo GetDamageInfo(CharacterBody target) {
            DamageInfo info = new();
            info.damage = damage * delay;
            info.attacker = owner != null ? owner.gameObject : null;
            info.procCoefficient = 0;
            info.position = target.corePosition;
            info.damageType = damageType;
            info.damageColorIndex = DamageColorIndex.Default;
            
            return info;
        }
    }

    public class DamageTrailPoint {
        public Vector3 location;
        public float lifetime;
        public GameObject effectInstance;
        public float totalLifetime;
    }
}