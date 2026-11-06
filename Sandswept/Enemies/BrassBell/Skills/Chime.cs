using System;
using System.Linq;
using RoR2.CharacterAI;
using Sandswept.Survivors;

namespace Sandswept.Enemies.BrassBell.States {
    public class StartChime : BaseSkillState {
        public float searchRadius = 55f;
        public static List<LazyIndex> AllyFilter = new() {
            new LazyIndex("BellBody"), new LazyIndex("BellConstructBody"),
        };
        public static BodyIndex BellIndex = new LazyIndex("BellConstructBody");
        public override void OnEnter()
        {
            base.OnEnter();

            if (base.isAuthority && (!base.characterBody.master.GetComponent<BaseAI>() || !base.characterBody.master.GetComponent<BaseAI>().currentEnemy.mainHurtBox)) {
                outer.SetNextStateToMain();
                return;
            }

            SphereSearch search = new();
            search.radius = searchRadius;
            search.origin = base.transform.position;
            search.mask = LayerIndex.entityPrecise.mask;
            search.RefreshCandidates();
            search.FilterCandidatesByDistinctHurtBoxEntities();
            HurtBox[] boxes = search.GetHurtBoxes();

            HurtBox target = base.characterBody.master.GetComponent<BaseAI>().currentEnemy.mainHurtBox;

            boxes = boxes.Where(x => x.teamIndex == GetTeam() && MatchesFilter(x.healthComponent.body.bodyIndex)).ToArray();

            if (boxes.Length <= 1)
            {
                outer.SetNextStateToMain();
                return;
            }

            foreach (HurtBox box in boxes)
            {
                if (base.isAuthority && box.healthComponent)
                {
                    EntityStateMachine weapon = EntityStateMachine.FindByCustomName(box.healthComponent.gameObject, "Weapon");

                    if (weapon != null)
                    {
                        weapon.SetState(new ChimeAttack(target));
                    }

                    if (box.healthComponent.body.bodyIndex == BellIndex)
                    {
                        box.healthComponent.body.skillLocator.secondary.temporaryCooldownPenalty = 8f;
                    }
                }
            }

            AkSoundEngine.PostEvent(Events.Play_PrayerBeads_LevelUp, base.gameObject);
            AkSoundEngine.PostEvent(Events.Play_bellBody_spawn, base.gameObject);
            
        }

        public bool MatchesFilter(BodyIndex index) {
            foreach (LazyIndex match in AllyFilter) {
                if (index == match) {
                    return true;
                }
            }

            return false;
        }

        public override void OnExit()
        {
            base.OnExit();
        }
    }

    public class ChimeAttack : BaseSkillState {
        public float duration = 0.7f;
        public float damageCoeff = 3.5f;
        public GameObject prepBall;
        public HurtBox target;
        public override void OnEnter()
        {
            base.OnEnter();

            var chime = GameObject.Instantiate(BrassBell.BellChimeEffect, base.gameObject.transform);
            chime.transform.localScale = Vector3.one * base.characterBody.bestFitRadius;
            prepBall = GameObject.Instantiate(GetPrepBall(), GetMuzzle());
            AkSoundEngine.PostEvent(Events.Play_bellBody_attackCreate, base.gameObject);

            PlayAnimation("Body", "Chime");
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= duration) {
                outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            base.OnExit();

            AkSoundEngine.PostEvent(Events.Play_bellBody_attackShoot, base.gameObject);

            if (prepBall) {
                GameObject.Destroy(prepBall);
            }

            if (base.isAuthority) {
                FireProjectileInfo info = new();
                info.owner = base.gameObject;
                info.damage = base.damageStat * damageCoeff;
                info.rotation = Util.QuaternionSafeLookRotation((target.transform.position - prepBall.transform.position).normalized);
                info.position = prepBall.transform.position;
                info.projectilePrefab = GetBall();
                info.crit = base.RollCrit();

                ProjectileManager.instance.FireProjectile(info);
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Stun;
        }

        public GameObject GetPrepBall() {
            if (base.characterBody.bodyIndex == BrassBellBody) {
                return BrassBell.BellShotPrep;
            }

            if (base.characterBody.bodyIndex == BrassContraptionBody) {
                return BCPB;
            }

            return BrassBell.BellShotPrep;
        }

        public GameObject GetBall() {
            if (base.characterBody.bodyIndex == BrassBellBody) {
                return BrassBell.BellShotProjectile;
            }

            if (base.characterBody.bodyIndex == BrassContraptionBody) {
                return BCB;
            }

            return BrassBell.BellShotProjectile;
        }

        public Transform GetMuzzle() {
            if (base.characterBody.bodyIndex == BrassBellBody) {
                return FindModelChild("Muzzle3");
            }

            if (base.characterBody.bodyIndex == BrassContraptionBody) {
                return FindModelChild("ProjectilePosition2");
            }

            return base.transform;
        }

        public static LazyAddressable<GameObject> BCPB = new(() => Paths.GameObject.PreppedBellBall);
        public static LazyAddressable<GameObject> BCB = new(() => Paths.GameObject.BellBall);
        public static LazyIndex BrassBellBody = new("BellConstructBody");
        public static LazyIndex BrassContraptionBody = new("BellBody");

        public ChimeAttack(HurtBox target) {
            this.target = target;
        }

        public ChimeAttack() {
            
        }
    }

    public class ChimeSkill : SkillBase<ChimeSkill> {
        public override string Name => "";

        public override string Description => "";

        public override Type ActivationStateType => typeof(StartChime);

        public override string ActivationMachineName => "Weapon";

        public override float Cooldown => 8f;

        public override Sprite Icon => null;
        public override bool BeginCooldownOnSkillEnd => true;
        public override bool CanceledFromSprinting => false;
    }
}