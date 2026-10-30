using System;
using RoR2.CharacterAI;
using Sandswept.Survivors;

namespace Sandswept.Enemies.ThetaConstruct
{
    public class CastShield : BaseSkillState
    {
        public GameObject shieldInstance;
        public float duration = 15f;
        public GameObject effect;
        public bool doYuriBlast = false;
        private Vector3 yuriDir;
        private GameObject buddy;

        public override void OnEnter()
        {
            base.OnEnter();

            BaseAI ai = base.characterBody.master ? base.characterBody.master.GetComponent<BaseAI>() : null;

            buddy = ai ? ai.buddy.gameObject : null;

            if (!buddy)
            {
                FailCast();
                return;
            }

            if (buddy.GetComponent<CharacterBody>() && buddy.GetComponent<CharacterBody>().bodyIndex == ThetaConstruct.ThetaIndex)
            {
                ai.buddy.gameObject = null;
                FailCast();
                return;
            }

            if (base.isAuthority)
            {
                Vector3 dir = (buddy.transform.position - base.transform.position).normalized;

                if (Physics.Raycast(base.transform.position, dir, (Vector3.Distance(base.transform.position, buddy.transform.position) * 0.75f), LayerIndex.world.mask))
                {
                    ai.buddy.gameObject = null;
                    FailCast();
                    return;
                }

                shieldInstance = GameObject.Instantiate(ThetaConstruct.ThetaShieldEffect, buddy.transform.position, Quaternion.identity);
                ThetaShieldController shieldController = shieldInstance.GetComponent<ThetaShieldController>();
                shieldController.targetHolder.ownerObject = buddy;
                shieldController.ownerHolder.ownerObject = base.gameObject;

                // RemoveBuff on exit used to leave the timed entry behind, and re-adding a non-stacking timed buff only refreshes that
                // entry without adding a stack, so a buddy shielded again within 15s had the shield visuals but no immunity
                CharacterBody buddyBody = buddy.GetComponent<CharacterBody>();
                buddyBody.ClearTimedBuffs(RoR2Content.Buffs.Immune);
                buddyBody.AddTimedBuff(RoR2Content.Buffs.Immune, duration);

                base.characterBody.master.GetComponent<BaseAI>().leader.gameObject = buddy;

                NetworkServer.Spawn(shieldInstance);
            }

            base.skillLocator.primary.DeductStock(1);

            Util.PlaySound("Play_treeBot_m1_impact", gameObject);
            Util.PlaySound("Play_engi_R_walkingTurret_laser_end", buddy);

            base.GetModelAnimator().SetBool("isShielding", true);
        }

        // failing used to leave the stock untouched, so the AI re-entered this state every single frame and ran a full
        // UpdateTargets search each time, which tanked performance whenever a theta had nothing valid to shield.
        // consume the stock and only wait a short retry delay instead of the full cooldown
        private void FailCast()
        {
            GenericSkill primary = base.skillLocator ? base.skillLocator.primary : null;
            if (primary && primary.stock > 0)
            {
                primary.DeductStock(1);
                primary.rechargeStopwatch = Mathf.Max(0f, primary.finalRechargeInterval - failRetryDelay);
            }

            shieldInstance = null;
            buddy = null;
            outer.SetNextStateToMain();
        }

        public const float failRetryDelay = 1.5f;

        public void DoYuriBlast()
        {
            doYuriBlast = false;

            EffectManager.SpawnEffect(Paths.GameObject.ExplosionMinorConstruct, new EffectData
            {
                origin = base.transform.position,
                scale = base.characterBody.bestFitRadius * 3.5f
            }, true);

            PhysForceInfo info = default;
            info.force = yuriDir * 90f;
            info.massIsOne = true;

            base.rigidbodyMotor.ApplyForceImpulse(in info);

            AkSoundEngine.PostEvent(Events.Play_majorConstruct_death, base.gameObject);
            AkSoundEngine.PostEvent(Events.Play_majorConstruct_R_pulse, base.gameObject);

            EffectManager.SpawnEffect(Paths.GameObject.ExplosionMinorConstruct, new EffectData
            {
                origin = shieldInstance.transform.position,
                scale = base.characterBody.bestFitRadius * 3.5f
            }, true);

            outer.SetNextStateToMain();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!base.isAuthority) return;

            if (shieldInstance && Vector3.Distance(base.characterBody.corePosition, shieldInstance.transform.position) > 90f)
            {
                outer.SetNextStateToMain();
            }

            if (base.fixedAge >= duration || !shieldInstance)
            {
                outer.SetNextStateToMain();
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Frozen;
        }

        public override void OnExit()
        {
            base.OnExit();

            base.GetModelAnimator().SetBool("isShielding", false);

            if (base.isAuthority && shieldInstance)
            {
                NetworkServer.DestroyObject(shieldInstance);
            }

            if (base.isAuthority && buddy)
            {
                // another theta may still be shielding the same buddy, don't take its immunity away
                if (!IsShieldedByOther(buddy, shieldInstance))
                {
                    buddy.gameObject.GetComponent<CharacterBody>().ClearTimedBuffs(RoR2Content.Buffs.Immune);
                }

                Util.PlaySound("Play_loader_R_expire", buddy);
            }

            Util.PlaySound("Play_loader_R_expire", gameObject);
        }

        public static bool IsShieldedByOther(GameObject target, GameObject ignoredShield)
        {
            foreach (ThetaShieldController shield in GameObject.FindObjectsOfType<ThetaShieldController>())
            {
                if (shield.gameObject != ignoredShield && shield.targetHolder && shield.targetHolder.ownerObject == target)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class CastShieldSkill : SkillBase<CastShieldSkill>
    {
        public override string Name => "";

        public override string Description => "";

        public override Type ActivationStateType => typeof(CastShield);

        public override string ActivationMachineName => "Weapon";

        public override float Cooldown => 10f;

        public override Sprite Icon => null;
        public override int StockToConsume => 0;
        public override bool BeginCooldownOnSkillEnd => true;
        public override bool CanceledFromSprinting => false;
    }
}