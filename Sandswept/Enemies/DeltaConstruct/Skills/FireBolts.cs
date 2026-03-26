using System;
using System.Collections;
using RoR2.CharacterAI;
using Sandswept.Survivors;

namespace Sandswept.Enemies.DeltaConstruct
{
    [ConfigSection("Enemies :: Delta Construct")]
    public class FireBolts : BaseSkillState
    {
        public float damageCoeff = 2f;
        public float duration = 2.4f;
        private Transform modelTransform;
        private Transform[] muzzles;

        [ConfigField("Bolt Projectile Damage", "Decimal.", 2f)]
        public static float projectileDamage;
        public AnimEventTracker anim;
        public Predictor[] predictors;
        public GameObject target;
        public static float projectileHorizontalSpeed = 40f;
        public static float projectileAntiGravity = -0.95f;

        public override void OnEnter()
        {
            base.OnEnter();

            damageCoeff = projectileDamage;

            duration /= base.attackSpeedStat;

            // base.characterMotor.walkSpeedPenaltyCoefficient = 0f;

            modelTransform = GetModelTransform();

            target = base.characterBody.master.GetComponent<BaseAI>().currentEnemy.gameObject;

            muzzles = [FindModelChild("Muzzle1"), FindModelChild("Muzzle2"), FindModelChild("Muzzle3"), FindModelChild("Muzzle4")];
            predictors = new Predictor[4];
            for (int i = 0; i < muzzles.Length; i++) {
                predictors[i] = new(muzzles[i]);
                predictors[i].SetTargetTransform(target.transform);
            }

            base.StartAimMode(0.2f);

            Util.PlaySound("Play_minorConstruct_attack_chargeUp", base.gameObject);
            Util.PlaySound("Play_minorConstruct_attack_chargeUp", base.gameObject);

            PlayAnimation("Gesture, Override", "Fire Cannons", "Generic.playbackRate", duration * 1.4f);

            anim = new(GetModelAnimator());
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override void Update()
        {
            base.Update();

            for (int i = 0; i < predictors.Length; i++) {
                predictors[i].Update();
            }

            if (anim.CheckEvent("Event.fire1")) FireBolt(3);
            if (anim.CheckEvent("Event.fire2")) FireBolt(1);
            if (anim.CheckEvent("Event.fire3")) FireBolt(2);
            if (anim.CheckEvent("Event.fire4")) FireBolt(0);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            base.StartAimMode(0.2f);

            if (base.fixedAge >= duration)
            {
                outer.SetNextStateToMain();
            }
        }

        public void ShowTelegraph(float duration)
        {

            if (modelTransform)
            {
                var temporaryOverlay = TemporaryOverlayManager.AddOverlay(modelTransform.gameObject);
                temporaryOverlay.duration = duration;
                temporaryOverlay.animateShaderAlpha = true;
                temporaryOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                temporaryOverlay.destroyComponentOnEnd = true;
                temporaryOverlay.originalMaterial = DeltaConstruct.matTell;
                temporaryOverlay.inspectorCharacterModel = modelTransform.GetComponent<CharacterModel>();
            }
        }

        public void FireBolt(int i)
        {
            Transform muzzle = muzzles[i];
            Predictor predictor = predictors[i];
            Transform target = predictor.GetTargetTransform();

            if (!target) {
                return;
            }

            Vector3 fireDirection = Vector3.zero;
            float speedOverride = projectileHorizontalSpeed;
            Vector3 vector = target.position - muzzle.position;
            vector.y = 0f;
            float magnitude = vector.magnitude;
            float num = Mathf.Max(0f, magnitude / projectileHorizontalSpeed);
            predictor.GetPredictedTargetPosition(num, out Vector3 predicted);
            fireDirection = Trajectory.CalculateInitialVelocityFromTime(muzzle.position, predicted, num, Physics.gravity.y * (1f - projectileAntiGravity), 0f, float.PositiveInfinity);
            speedOverride = fireDirection.magnitude;

            FireProjectileInfo info = new();
            info.crit = base.RollCrit();
            info.damage = base.damageStat * damageCoeff;
            info.rotation = Util.QuaternionSafeLookRotation(fireDirection.normalized);
            info.position = muzzle.position;
            info.owner = base.gameObject;
            info.projectilePrefab = DeltaConstruct.bolt;
            info.speedOverride = speedOverride;

            if (NetworkServer.active)
            {
                ProjectileManager.instance.FireProjectile(info);
            }

            EffectManager.SpawnEffect(DeltaConstruct.muzzleFlash, new EffectData
            {
                rotation = Util.QuaternionSafeLookRotation(muzzle.up),
                origin = muzzle.position
            }, false);

            Util.PlaySound("Play_minorConstruct_attack_shoot", base.gameObject);
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Frozen;
        }
    }

    public class FireBoltsSkill : SkillBase<FireBoltsSkill>
    {
        public override string Name => "";

        public override string Description => "";

        public override Type ActivationStateType => typeof(FireBolts);

        public override string ActivationMachineName => "Weapon";

        public override float Cooldown => 4f;

        public override Sprite Icon => null;
        public override bool BeginCooldownOnSkillEnd => true;
        public override bool CanceledFromSprinting => false;
    }
}