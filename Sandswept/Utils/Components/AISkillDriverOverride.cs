using System;
using System.Linq;
using RoR2.CharacterAI;
using static RoR2.CharacterAI.BaseAI;

namespace Sandswept {
    public abstract class AISkillDriverSelectionOverride : MonoBehaviour {
        public BaseAI ai;
        private AISkillDriver[] drivers = null;
        public abstract SkillDriverEvaluation EvaluateSkillDrivers();
        private Dictionary<string, AISkillDriver> DriverMap = new();
        public AISkillDriver FindDriver(string name) {
            if (drivers == null) {
                drivers = GetComponents<AISkillDriver>();
            }
            
            if (DriverMap.ContainsKey(name)) {
                return DriverMap[name];
            }
            else {
                AISkillDriver driver = drivers.First(x => x.customName == name);
                DriverMap.Add(name, driver);
                
                return driver;
            }
        }

        public SkillDriverEvaluation DefaultEvalForDriver(AISkillDriver driver) {
            SkillDriverEvaluation eval = default;
            eval.dominantSkillDriver = driver;
            eval.target = FindMoveTarget(driver.moveTargetType);
            eval.aimTarget = FindAimTarget(driver.aimType, eval.target);

            return eval;
        }

        public Target FindMoveTarget(AISkillDriver.TargetType targetType, float minDist = 0f, float maxDist = 180f) {
            switch (targetType) {
                case AISkillDriver.TargetType.CurrentEnemy:
                    return ai.currentEnemy;
                case AISkillDriver.TargetType.CurrentLeader:
                    return ai.leader;
                case AISkillDriver.TargetType.NearestFriendlyInSkillRange:
                    ai.buddySearch.teamMaskFilter = TeamMask.none;
                    ai.buddySearch.teamMaskFilter.AddTeam(ai.master.teamIndex);
                    ai.buddySearch.sortMode = BullseyeSearch.SortMode.Distance;
                    ai.buddySearch.minDistanceFilter = minDist;
                    ai.buddySearch.maxDistanceFilter = maxDist;
                    ai.buddySearch.searchOrigin = ai.bodyInputBank.aimOrigin;
                    ai.buddySearch.searchDirection = ai.bodyInputBank.aimDirection;
                    ai.buddySearch.maxAngleFilter = 180f;
                    ai.buddySearch.RefreshCandidates();

                    if (ai.body)
                    {
                        ai.buddySearch.FilterOutGameObject(ai.body.gameObject);
                    }
    
                    HurtBox hurtBox = ai.buddySearch.GetResults().FirstOrDefault();
                    if (hurtBox && hurtBox.healthComponent)
                    {
                        ai.buddy.gameObject = hurtBox.healthComponent.gameObject;
                        ai.buddy.bestHurtBox = hurtBox;
                    }

                    return ai.buddy;
                case AISkillDriver.TargetType.Custom:
                    return ai.customTarget;
                default:
                    return ai.currentEnemy;
            }
        }

        public Target FindAimTarget(AISkillDriver.AimType targetType, Target moveTarget) {
            switch (targetType) {
                case AISkillDriver.AimType.AtCurrentEnemy:
                    return ai.currentEnemy;
                case AISkillDriver.AimType.AtCurrentLeader:
                    return ai.leader;
                case AISkillDriver.AimType.AtMoveTarget:
                    return moveTarget;
                default:
                    return moveTarget;
            }
        }

        public virtual void Start() {
            ai = GetComponent<BaseAI>();

            DriverOverrideMap.Add(ai, this);
        }

        public virtual void OnDestroy() {
            DriverOverrideMap.Remove(ai);
        }

        public static void Initialize() {
            On.RoR2.CharacterAI.BaseAI.EvaluateSkillDrivers += OnEvaluateSkillDrivers;
        }

        public static Dictionary<BaseAI, AISkillDriverSelectionOverride> DriverOverrideMap = new();
        private static SkillDriverEvaluation OnEvaluateSkillDrivers(On.RoR2.CharacterAI.BaseAI.orig_EvaluateSkillDrivers orig, BaseAI self)
        {
            if (DriverOverrideMap.ContainsKey(self)) {
                return DriverOverrideMap[self].EvaluateSkillDrivers();    
            }

            return orig(self);
        }
    }
}