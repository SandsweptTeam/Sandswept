using System;
using System.Diagnostics;

namespace Sandswept.Utils {
    public class DamageTrailManager : MonoBehaviour {
        public static List<DamageTrail> activeDamageTrails = new();
        public static List<CharacterBody> bodies => CharacterBody.instancesList;
        public static DamageTrailManager instance;

        public static DamageTrail DeployDamageTrail(CharacterBody owner, float damage, GameObject effect, float radius, float tickRate = 5f, DamageTypeCombo damageType = default) {
            DamageTrail trail = new();
            trail.owner = owner;
            trail.damage = owner.damage * damage;
            trail.delay = 1f / tickRate;
            trail.pointEffect = effect;
            trail.damage = damage;
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
        }

        public void FixedUpdate() {
            foreach (DamageTrail trail in activeDamageTrails) {
                trail.stopwatch += Time.fixedDeltaTime;

                if (trail.stopwatch >= trail.delay) {
                    trail.stopwatch = 0f;

                    foreach (CharacterBody body in bodies) {
                        if (!body.hasAuthority) continue;
                        if (body.teamComponent.teamIndex == trail.team) continue; 
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

        public DamageInfo GetDamageInfo(CharacterBody target) {
            DamageInfo info = new();
            info.damage = damage * delay;
            info.attacker = owner != null ? owner.gameObject : null;
            info.procCoefficient = 0;
            info.position = target.corePosition;
            
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