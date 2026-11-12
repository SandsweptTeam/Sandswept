using System;
using System.Diagnostics.Contracts;
using System.Linq;
using RoR2.CharacterAI;
using Sandswept.Buffs;
using Sandswept.Survivors;

namespace Sandswept.Enemies.BrassChoir.States {
    public class BellAura : BaseSkillState {
        public float duration = 15f;
        public float stopwatch = 0f;
        public static List<LazyIndex> AllyFilter = new() {
            new LazyIndex("BellBody"), new LazyIndex("BellConstructBody"),
        };
        public List<string> AvailableBells = new List<string>() {
            "Bell1", "Bell2", "Bell3", "Bell4", "Bell5", "Bell6", "Bell7", "Bell8", "Bell9"
        };
        public List<TetherInfo> tethers = new();
        public class TetherInfo {
            public GameObject instance;
            public HurtBox target;
            private Transform end;
            public TetherInfo(GameObject instance, HurtBox target) {
                this.instance = instance;
                this.target = target;

                end = instance.GetComponent<ChildLocator>().FindChild("End");
            }

            public void Update() {
                if (target != null && target.healthComponent && target.healthComponent.alive) {
                    end.transform.position = target.transform.position;
                }
                else {
                    Destroy();
                }
            }

            public void Destroy() {
                if (instance) {
                    GameObject.Destroy(instance);
                }
            }
        }
        public override void OnEnter()
        {
            base.OnEnter();

            if (base.isAuthority) {
                BrassChoir.ReplicateToSpires(base.characterBody, SkillSlot.Special);
            }

            PlayAnimation("Body", "Ready Dance");
            GetModelAnimator().SetBool("isDancing", true);
        }

        public override void Update()
        {
            base.Update();

            foreach (TetherInfo tether in tethers) {
                tether.Update();
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= duration) {
                outer.SetNextState(new BellAuraRecoil());
            }

            stopwatch += Time.fixedDeltaTime;

            if (stopwatch >= 1f) {
                stopwatch = 0f;

                AttemptTether();
                if (base.isAuthority) {
                    foreach (TetherInfo info in tethers) {
                        if (info.target != null && info.target.healthComponent) {
                            info.target.healthComponent.body.SetBuffCount(BellBoost.instance.BuffDef.buffIndex, 1);
                        }
                    }
                }
            }
        }

        public void AttemptTether() {
            if (AvailableBells.Count == 0) {
                return;
            }

            SphereSearch search = new();
            search.radius = 70f;
            search.origin = base.transform.position;
            search.mask = LayerIndex.entityPrecise.mask;
            search.RefreshCandidates();
            search.FilterCandidatesByDistinctHurtBoxEntities();

            HurtBox[] boxes = search.GetHurtBoxes();

            boxes = boxes.Where(x => x.teamIndex == GetTeam() && MatchesFilter(x.healthComponent.body.bodyIndex)).ToArray();

            if (boxes.Length <= 1)
            {
                outer.SetNextStateToMain();
                return;
            }

            foreach (HurtBox box in boxes)
            {
                if (box.healthComponent)
                {
                    if (AvailableBells.Count == 0) {
                        continue;
                    }

                    string bell = AvailableBells.ElementAt(0);
                    AvailableBells.Remove(bell);

                    GameObject effect = GameObject.Instantiate(BrassChoir.BellTether, FindModelChild(bell));
                    tethers.Add(new TetherInfo(effect, box));
                }
            }
        }

        private bool MatchesFilter(BodyIndex index)
        {
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

            foreach (TetherInfo tether in tethers) {
                tether.Destroy();
            }

            GetModelAnimator().SetBool("isDancing", false);

            if (base.isAuthority) {
                foreach (TetherInfo info in tethers) {
                    if (info.target != null && info.target.healthComponent) {
                        info.target.healthComponent.body.SetBuffCount(BellBoost.instance.BuffDef.buffIndex, 0);
                    }
                }
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death;
        }
    }

    public class BellAuraRecoil : BaseSkillState {
        public float duration = 5f;

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= duration) {
                outer.SetNextStateToMain();
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death;
        }
    }

    public class BellAuraSkill : SkillBase<BellAuraSkill> {
        public override string Name => "";

        public override string Description => "";

        public override Type ActivationStateType => typeof(BellAura);

        public override string ActivationMachineName => "Weapon";

        public override float Cooldown => 25f;
        public override Sprite Icon => null;
        public override bool BeginCooldownOnSkillEnd => true;
        public override bool CanceledFromSprinting => false;
        public override bool FullRestockOnAssign => false;
    }
}