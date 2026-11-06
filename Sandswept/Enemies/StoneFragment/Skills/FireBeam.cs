using System;
using Sandswept.Survivors;

namespace Sandswept.Enemies.StoneFragment
{
    [ConfigSection("Enemies :: Stone Fragment")]
    public class FireBeam : BaseSkillState
    {
        public Animator anim;
        public float duration = 0.45f;
        public float windUp = 0.8f;
        public bool firing = false;
        public BasicLaserBeam laser;
        private Transform modelTransform;
        public static float damageCoeff = 5f;
        private GameObject chargeEffect;

        public override void OnEnter()
        {
            base.OnEnter();

            modelTransform = GetModelTransform();

            AkSoundEngine.PostEvent(Events.Play_golem_laser_charge, base.gameObject);

            chargeEffect = GameObject.Instantiate(Paths.GameObject.ChargeGolem, FindModelChild("MuzzleLaser"));
            chargeEffect.transform.localPosition = Vector3.zero;
            chargeEffect.EditComponent<ScaleParticleSystemDuration>((x) => {
                x.newDuration = windUp;
            });

            laser = new(base.characterBody, FindModelChild("MuzzleLaser"),
                new BasicLaserInfo
                {
                    OriginIsBase = true,
                    EndpointName = "End",
                    DamageCoefficient = damageCoeff,
                    FiringWidthMultiplier = 1f,
                    MaxRange = 200f,
                    FiringMaterial = StoneFragment.matFragmentBeam,
                    ChargeDelay = windUp,
                    EffectPrefab = StoneFragment.FragmentBeam,
                    FiringMode = LaserFiringMode.TrackAim,
                    ImpactEffect = null,
                    TickRate = 8f,
                    DontOffsetEndpoint = true
                }
            );

            anim = GetModelAnimator();
        }

        public override void Update()
        {
            base.Update();

            laser.UpdateVisual(Time.deltaTime);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            laser.Update(Time.fixedDeltaTime);

            base.StartAimMode(0.2f);

            if (base.fixedAge >= windUp && !firing) {
                firing = true;
                laser.Fire();
                AkSoundEngine.PostEvent(Events.Play_titanboss_R_laser_loop, base.gameObject);
            }

            if (base.fixedAge >= windUp + duration) {
                outer.SetNextStateToMain();
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Stun;
        }

        public override void OnExit()
        {
            base.OnExit();
            laser.Stop();
            AkSoundEngine.PostEvent(Events.Stop_titanboss_R_laser_loop, base.gameObject);
            if (chargeEffect) {
                GameObject.Destroy(chargeEffect);
            }
        }
    }

    public class FireBeamSkill : SkillBase<FireBeamSkill>
    {
        public override string Name => "";

        public override string Description => "";

        public override Type ActivationStateType => typeof(FireBeam);

        public override string ActivationMachineName => "Weapon";

        public override float Cooldown => 6f;

        public override Sprite Icon => null;
        public override bool BeginCooldownOnSkillEnd => true;
        public override bool CanceledFromSprinting => false;
    }
}