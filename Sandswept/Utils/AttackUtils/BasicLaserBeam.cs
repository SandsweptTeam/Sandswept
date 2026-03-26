using System;
using Sandswept.Utils.Components;

namespace Sandswept.Utils
{
    public class BasicLaserBeam
    {
        public bool Active => firing;
        public float DamageCoefficient;
        public CharacterBody Owner;
        public Transform TargetMuzzle;
        private LineRenderer lr;
        public GameObject effectInstance;
        private bool firing = false;
        private float stopwatch = 0f;
        private float growthStopwatch = 0f;
        private float delay;
        public BasicLaserInfo info;
        private Transform origin;
        private Transform end;
        private Vector3 targetEndpoint;
        private Vector3 lastEndpoint;
        private float origWidth = 0f;
        private float stopwatch2 = 0f;
        private List<CharacterBody> alreadyDamaged = new();

        public BasicLaserBeam(CharacterBody owner, Transform muzzle, BasicLaserInfo info)
        {
            this.info = info;
            delay = 1f / info.TickRate;
            TargetMuzzle = muzzle;
            DamageCoefficient = info.SingleHit ? info.DamageCoefficient : info.DamageCoefficient * delay;
            effectInstance = GameObject.Instantiate(info.EffectPrefab, muzzle.transform.position, Quaternion.identity);
            growthStopwatch = info.ChargeDelay;
            origin = info.OriginIsBase ? effectInstance.transform : effectInstance.GetComponent<ChildLocator>().FindChild(info.OriginName);
            end = effectInstance.GetComponent<ChildLocator>().FindChild(info.EndpointName);
            lr = effectInstance.GetComponent<DetachLineRendererAndFade>().line;
            origWidth = lr.widthMultiplier;
            Owner = owner;

            targetEndpoint = GetEndpoint(out _);
            lastEndpoint = targetEndpoint;
            end.transform.position = targetEndpoint;
            origin.transform.position = TargetMuzzle.transform.position;
        }

        public void Fire()
        {
            if (info.FiringMaterial)
            {
                lr.material = info.FiringMaterial;
            }

            lr.widthMultiplier = origWidth * info.FiringWidthMultiplier;
            firing = true;
            stopwatch = 0f;
        }

        public void UpdateVisual(float deltaTime)
        {
            stopwatch2 += deltaTime;

            origin.transform.position = TargetMuzzle.transform.position;
            end.transform.position = Vector3.Lerp(lastEndpoint, targetEndpoint, stopwatch2 / delay);

            if (stopwatch2 >= delay) {
                stopwatch2 = 0f;
            }

            if (!firing)
            {
                growthStopwatch -= deltaTime;
                lr.widthMultiplier = Mathf.Max(0f, (growthStopwatch / info.ChargeDelay));
            }
        }

        public void Update(float deltaTime)
        {
            stopwatch += deltaTime;

            if (stopwatch >= delay)
            {
                lastEndpoint = targetEndpoint;
                targetEndpoint = GetEndpoint(out Vector3 impact);
                stopwatch = 0f;

                if (firing && Owner.hasAuthority)
                {
                    GetBulletAttack().Fire();

                    if (info.ImpactEffect)
                    {
                        EffectManager.SpawnEffect(info.ImpactEffect, new EffectData
                        {
                            origin = impact,
                            scale = 1f
                        }, false);
                    }

                    info.ImpactCallback?.Invoke(impact);
                }
            }
        }

        public Vector3 GetEndpoint(out Vector3 unmodified)
        {
            Vector3 dir = (info.FiringMode == LaserFiringMode.TrackAim) ? Owner.inputBank.aimDirection : info.UseUP ? TargetMuzzle.up : TargetMuzzle.forward;
            Vector3 pos = (info.FiringMode == LaserFiringMode.TrackAim) ? Owner.inputBank.aimOrigin : TargetMuzzle.position;
            Vector3 endpoint = new Ray(pos, dir).GetPoint(info.MaxRange);

            if (Physics.Raycast(pos, dir, out RaycastHit hit, info.MaxRange, LayerIndex.world.mask))
            {
                endpoint = hit.point;
            }

            unmodified = endpoint;
            endpoint += dir * 5f;

            return endpoint;
        }

        public BulletAttack GetBulletAttack()
        {
            BulletAttack attack = new();
            attack.radius = lr.startWidth * 0.75f;
            attack.damage = Owner.damage * DamageCoefficient;
            attack.origin = (info.FiringMode == LaserFiringMode.TrackAim) ? Owner.inputBank.aimOrigin : TargetMuzzle.position;
            attack.aimVector = (end.transform.position - TargetMuzzle.position).normalized;
            attack.procCoefficient = 1f;
            attack.owner = Owner.gameObject;
            attack.falloffModel = BulletAttack.FalloffModel.None;
            attack.isCrit = Util.CheckRoll(Owner.crit, Owner.master);
            attack.stopperMask = LayerIndex.world.mask;
            attack.hitCallback = (BulletAttack attack, ref BulletAttack.BulletHit hit) => {
                bool res = BulletAttack.defaultHitCallback(attack, ref hit);

                if (info.SingleHit && hit.hurtBox && hit.hurtBox.healthComponent) {
                    if (alreadyDamaged.Contains(hit.hurtBox.healthComponent.body)) {
                        return false;
                    }

                    alreadyDamaged.Add(hit.hitHurtBox.healthComponent.body);
                }

                return res;
            };

            return attack;
        }

        public void Stop()
        {
            GameObject.Destroy(effectInstance);
        }
    }

    public class BasicLaserInfo
    {
        public GameObject EffectPrefab;
        public string OriginName = "Origin";
        public string EndpointName = "End";
        public bool OriginIsBase = true;
        public float TickRate = 20f;
        public float DamageCoefficient = 1f;
        public Material FiringMaterial;
        public float ChargeDelay = 0.5f;
        public float FiringWidthMultiplier = 2f;
        public LaserFiringMode FiringMode = LaserFiringMode.Straight;
        public float MaxRange = 60f;
        public GameObject ImpactEffect;
        public bool SingleHit = false;
        public bool UseUP = false;
        public Action<Vector3> ImpactCallback;
    }

    public enum LaserFiringMode
    {
        TrackAim,
        Straight
    }
}