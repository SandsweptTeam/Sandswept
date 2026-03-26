using System;
using Rewired.Demos;
using RoR2.ConVar;
using Sandswept.Survivors;
using Sandswept.Utils.Components;
using DamageTrail = Sandswept.Utils.DamageTrail;

namespace Sandswept.Enemies.DeltaConstruct
{
    public class SkystrikeIntro : BaseSkillState
    {
        public float duration = 0.7f;

        public override void OnEnter()
        {
            base.OnEnter();

            base.characterMotor.walkSpeedPenaltyCoefficient = 0f;

            base.gameObject.layer = LayerIndex.noCollision.intVal;
            base.characterMotor.Motor.RebuildCollidableLayers();

            PlayAnimation("Body", "Leap", "Generic.playbackRate", duration);
            

            GetModelAnimator().SetLayerWeight(GetModelAnimator().GetLayerIndex("AimYaw"), 0f);
            GetModelAnimator().SetLayerWeight(GetModelAnimator().GetLayerIndex("AimPitch"), 0f);

            base.characterMotor.ApplyForce(Vector3.up * base.characterMotor.mass * 40f, true, true);

            Util.PlaySound("Play_moonBrother_phaseJump_kneel", base.gameObject);
            // Util.PlaySound("Play_moonBrother_phaseJump_jumpAway", base.gameObject);
            Util.PlaySound("Play_majorConstruct_shift_raise", gameObject);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= duration)
            {
                outer.SetNextState(new SkystrikeTransform());
            }
        }

        public override void OnExit()
        {
            base.gameObject.layer = LayerIndex.defaultLayer.intVal;
            base.characterMotor.Motor.RebuildCollidableLayers();

            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Stun;
        }
    }

    [ConfigSection("Enemies :: Delta Construct")]
    public class SkystrikeFire : BaseSkillState
    {
        public float duration = 3.45f;
        public float delay = 1f / 20f;
        public float damageCoeff = 12f / 10f;
        public float stopwatch = 0f;
        public BasicLaserBeam[] skystrikeBeams;
        public float speed = 40f;
        public Vector3 guh;

        [ConfigField("Laser Configuration Speed", "In m/s", 40f)]
        public static float laserSpeed;

        [ConfigField("Laser Configuration Damage", "Decimal.", 1.2f)]
        public static float laserDamage;

        [ConfigField("Fire Trail Damage Per Second", "Decimal.", 4f)]
        public static float fireTrailDPS;
        public static float fireLifetime = 8f;
        public static float fireRadius = 2f;
        public DamageTrail trail;

        public override void OnEnter()
        {
            base.OnEnter();

            speed = laserSpeed;
            damageCoeff = laserDamage;

            trail = DamageTrailManager.DeployDamageTrail(base.characterBody, fireTrailDPS, DeltaConstruct.DeltaBurnyTrail, fireRadius);
            trail.mergeRadius = 15;
            trail.minimumMergeTime = 1;

            Util.PlaySound("Play_majorConstruct_m1_laser_loop", gameObject);

            PlayAnimation("Body", "Skystrike Fire", "Generic.playbackRate", duration);

            for (int i = 0; i < skystrikeBeams.Length; i++)
            {
                skystrikeBeams[i].Fire();
                skystrikeBeams[i].info.ImpactCallback = (pos) => {
                    DamageTrailManager.AddSegment(trail, pos, fireLifetime, resetAllLifetime: true);
                };
            }
        }

        public override void Update()
        {
            base.Update();

            for (int i = 0; i < skystrikeBeams.Length; i++)
            {
                BasicLaserBeam beam = skystrikeBeams[i];
                beam.UpdateVisual(Time.deltaTime);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            stopwatch += Time.fixedDeltaTime;

            base.characterMotor.velocity = Vector3.zero;

            for (int i = 0; i < skystrikeBeams.Length; i++)
            {
                BasicLaserBeam beam = skystrikeBeams[i];
                beam.Update(Time.fixedDeltaTime);
            }

            if (base.fixedAge >= duration)
            {
                outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            base.OnExit();

            DamageTrailManager.DestroyTrail(trail);

            base.characterMotor.walkSpeedPenaltyCoefficient = 1f;

            base.characterDirection.enabled = true;

            for (int i = 0; i < skystrikeBeams.Length; i++)
            {
                skystrikeBeams[i].Stop();
            }

            Util.PlaySound("Stop_majorConstruct_m1_laser_loop", gameObject);
            Util.PlaySound("Play_majorConstruct_m1_laser_end", gameObject);

            GetModelAnimator().SetBool("isAerial", false);
            // PlayAnimation("Body", "Aerial To Ground", "Generic.playbackRate", duration);
            GetModelAnimator().SetLayerWeight(GetModelAnimator().GetLayerIndex("AimYaw"), 1f);
            GetModelAnimator().SetLayerWeight(GetModelAnimator().GetLayerIndex("AimPitch"), 1f);
        }

        public BulletAttack GetBulletAttack(SkystrikeLaserInfo info)
        {
            BulletAttack attack = new()
            {
                radius = 1.2f,
                damage = base.damageStat * damageCoeff,
                origin = info.muzzle.position,
                aimVector = info.muzzle.forward.normalized,
                procCoefficient = 0.1f,
                owner = base.gameObject,
                falloffModel = BulletAttack.FalloffModel.None,
                isCrit = base.RollCrit(),
                stopperMask = LayerIndex.world.mask
            };

            return attack;
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death;
        }
    }

    public class SkystrikeWindup : BaseSkillState
    {
        public float duration = 0.7f;
        public BasicLaserBeam[] skystrikeBeams;
        public Vector3 guh;
        public bool wasKnockedOutOfState = true;

        public override void OnEnter()
        {
            base.OnEnter();

            skystrikeBeams = new BasicLaserBeam[8];

            for (int i = 0; i < skystrikeBeams.Length; i++)
            {
                BasicLaserBeam beam = new(base.characterBody, FindModelChild("Muzzle" + (i + 1)), new BasicLaserInfo() {
                    OriginIsBase = true,
                    EndpointName = "End",
                    DamageCoefficient = SkystrikeFire.laserDamage,
                    FiringWidthMultiplier = 2.2f,
                    MaxRange = 190f,
                    FiringMaterial = DeltaConstruct.matDeltaBeamStrong,
                    ChargeDelay = duration,
                    EffectPrefab = DeltaConstruct.beam,
                    FiringMode = LaserFiringMode.Straight,
                    ImpactEffect = DeltaConstruct.muzzleFlash,
                    TickRate = 20f,
                    SingleHit = false,
                    UseUP = true,
                });

                skystrikeBeams[i] = beam;
            }

            Util.PlaySound("Play_majorConstruct_m1_laser_chargeShoot", base.gameObject);
            Util.PlaySound("Play_majorConstruct_m1_laser_chargeShoot", base.gameObject);
            // Util.PlaySound("Play_majorConstruct_m1_laser_chargeShoot", base.gameObject);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= duration)
            {
                wasKnockedOutOfState = false;
                outer.SetNextState(new SkystrikeFire());
            }

            if (base.characterMotor.velocity.y < 0)
            {
                base.characterMotor.velocity.y = 0;
            }

            base.characterMotor.velocity = new(0, base.characterMotor.velocity.y, 0);
        }

        public override void OnExit()
        {
            base.OnExit();

            if (!wasKnockedOutOfState) return;

            for (int i = 0; i < skystrikeBeams.Length; i++)
            {
                skystrikeBeams[i].Stop();
            }

            Util.PlaySound("Stop_majorConstruct_m1_laser_loop", gameObject);
            Util.PlaySound("Play_majorConstruct_m1_laser_end", gameObject);
        }

        public override void ModifyNextState(EntityState nextState)
        {
            base.ModifyNextState(nextState);

            if (nextState is SkystrikeFire skystrikeFire)
            {
                skystrikeFire.skystrikeBeams = skystrikeBeams;
                skystrikeFire.guh = guh;
                // why would you name a variable that...
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death;
        }
    }

    public class SkystrikeTransform : BaseSkillState
    {
        public float duration = 0.7f;
        public Vector3 dir;

        public override void OnEnter()
        {
            base.OnEnter();

            dir = base.characterDirection.forward;

            base.characterDirection.enabled = false;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            // characterDirection.forward = dir;

            if (base.fixedAge >= duration)
            {
                outer.SetNextState(new SkystrikeWindup());
            }

            if (base.characterMotor.velocity.y < 0)
            {
                base.characterMotor.velocity.y = 0;
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death;
        }
    }

    public class SkystrikeLaserInfo
    {
        public Transform muzzle;
        public GameObject effect;
        public Transform lineHandle;
        public LineRenderer rend;
    }

    public class SkystrikeSkill : SkillBase<SkystrikeSkill>
    {
        public override string Name => "omg hiii";

        public override string Description => "<3 :3 :3 <3 UwU >w< >_< >_> OwO :3 <3";

        public override Type ActivationStateType => typeof(SkystrikeIntro);

        public override string ActivationMachineName => "Body";

        public override float Cooldown => 20f;

        public override Sprite Icon => null;
        public override bool BeginCooldownOnSkillEnd => true;
        public override bool CanceledFromSprinting => false;
    }
}