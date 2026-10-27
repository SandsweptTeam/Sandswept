using System;
using RoR2.CharacterAI;
using static RoR2.CharacterAI.BaseAI;

namespace Sandswept.Enemies.Ivy {
    public class IvyAIController : AISkillDriverSelectionOverride
    {
        public IvyHeadAIController head;
        public void FixedUpdate() {
            if (!head && ai && ai.body) {
                head = (EntityStateMachine.FindByCustomName(ai.body.gameObject, "Body").state as IvyMainState).IvyHeadBody.master.GetComponent<IvyHeadAIController>();
            }
        }
        public override SkillDriverEvaluation EvaluateSkillDrivers()
        {
            Target enemy = ai.currentEnemy;
            Target leader = ai.leader;
            Target custom = ai.customTarget;

            /*if (custom.gameObject != null) {
                float dist = Vector3.Distance(ai.body.gameObject.transform.position, leader.gameObject.transform.position);

                if (dist <= grabRange) {
                    state = "grabbing";
                    return DefaultEvalForDriver(GrabTarget);
                }
                else {
                    state = "grab chase";
                    return DefaultEvalForDriver(ChaseGrabTarget);
                }
            }*/

            return DefaultEvalForDriver(Idle);
        }

        public AISkillDriver DeployHead => FindDriver("Deploy Head");
        public AISkillDriver ChaseAlly => FindDriver("Enter Deploy Range");
        public AISkillDriver FleeEnemy => FindDriver("Flee Enemy");
        public AISkillDriver StrafeEnemy => FindDriver("Strafe Enemy");
        public AISkillDriver ChaseEnemy => FindDriver("Chase Enemy");
        public AISkillDriver Idle => FindDriver("Idle");
    }
    public class IvyHeadAIController : AISkillDriverSelectionOverride
    {
        public IvyAIController ivyController;
        public float maxHeadRange = 35f;
        public float maxHeadStretchTime = 2.5f;
        private float headStretchTimer = 0f;
        public float chaseDistance = 15f;
        public bool outOfRange = false;
        public float grabRange = 8f;
        public string state = "";
        public override SkillDriverEvaluation EvaluateSkillDrivers()
        {
            Target enemy = ai.currentEnemy;
            Target leader = ai.leader;
            Target custom = ai.customTarget;

            if (custom.gameObject != null) {
                float dist = Vector3.Distance(ai.body.gameObject.transform.position, leader.gameObject.transform.position);

                Debug.LogError("dist from target is " + dist);

                if (dist <= grabRange) {
                    state = "grabbing";
                    return DefaultEvalForDriver(GrabTarget);
                }
                else {
                    state = "grab chase";
                    return DefaultEvalForDriver(ChaseGrabTarget);
                }
            }

            if (leader.gameObject != null) {
                float dist = Vector3.Distance(ai.body.gameObject.transform.position, leader.gameObject.transform.position);

                if (dist > maxHeadRange) {
                    outOfRange = true;

                    if (headStretchTimer >= maxHeadStretchTime) {
                        state = "tether oor";
                        return DefaultEvalForDriver(TetherToBody);
                    }
                }
                else {
                    outOfRange = false;
                    headStretchTimer = 0f;

                    if (enemy.gameObject != null) {
                        float enemyDist = Vector3.Distance(ai.body.gameObject.transform.position, enemy.gameObject.transform.position);

                        if (enemyDist >= chaseDistance) {
                            state = "chase enemy";
                            return DefaultEvalForDriver(ChaseEnemy);
                        }
                        else {
                            state = "strafe";
                            return DefaultEvalForDriver(StrafeLeader);
                        }
                    }
                    else {
                        state = "wander";
                        return DefaultEvalForDriver(DetachLeader);
                    }
                }
            }

            state = "tether def";
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