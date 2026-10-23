using System;
using RoR2.CharacterAI;
using static RoR2.CharacterAI.BaseAI;

namespace Sandswept.Enemies.Ivy {
    public class IvyAIController : AISkillDriverSelectionOverride
    {
        public override SkillDriverEvaluation EvaluateSkillDrivers()
        {
            return default;
        }
    }
    public class IvyHeadAIController : AISkillDriverSelectionOverride
    {
        public IvyAIController ivyController;
        public float maxHeadRange = 35f;
        public float maxHeadStretchTime = 2.5f;
        private float headStretchTimer = 0f;
        private float chaseDistance = 15f;
        private bool outOfRange = false;
        private float grabRange = 5f;
        public override SkillDriverEvaluation EvaluateSkillDrivers()
        {
            Target enemy = ai.currentEnemy;
            Target leader = ai.leader;
            Target custom = ai.customTarget;

            if (custom.gameObject != null) {
                float dist = Vector3.Distance(ai.body.gameObject.transform.position, leader.gameObject.transform.position);

                if (dist <= grabRange) {
                    return DefaultEvalForDriver(GrabTarget);
                }
                else {
                    return DefaultEvalForDriver(ChaseGrabTarget);
                }
            }

            if (leader.gameObject != null) {
                float dist = Vector3.Distance(ai.body.gameObject.transform.position, leader.gameObject.transform.position);

                if (dist > maxHeadRange) {
                    outOfRange = true;

                    if (headStretchTimer >= maxHeadStretchTime) {
                        return DefaultEvalForDriver(TetherToBody);
                    }
                }
                else {
                    outOfRange = false;
                    headStretchTimer = 0f;

                    if (enemy.gameObject != null) {
                        float enemyDist = Vector3.Distance(ai.body.gameObject.transform.position, enemy.gameObject.transform.position);

                        if (enemyDist >= chaseDistance) {
                            return DefaultEvalForDriver(ChaseEnemy);
                        }
                        else {
                            return DefaultEvalForDriver(StrafeLeader);
                        }
                    }
                    else {
                        return DefaultEvalForDriver(DetachLeader);
                    }
                }
            }

            return DefaultEvalForDriver(TetherToBody);
        }

        public void FixedUpdate() {
            if (outOfRange) {
                headStretchTimer += Time.fixedDeltaTime;
            }
        }

        public AISkillDriver GrabTarget => FindDriver("Grab Target");
        public AISkillDriver ChaseGrabTarget => FindDriver("Chase Grab Target");
        public AISkillDriver ChaseEnemy => FindDriver("Chase Enemy");
        public AISkillDriver StrafeEnemy => FindDriver("Strafe Enemy");
        public AISkillDriver TetherToBody => FindDriver("Tether To Body");
        public AISkillDriver StrafeLeader => FindDriver("Strafe Leader");
        public AISkillDriver DetachLeader => FindDriver("Detach Leader");
    }
}