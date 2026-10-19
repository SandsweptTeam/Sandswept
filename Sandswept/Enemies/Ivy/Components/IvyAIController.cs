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
        public override SkillDriverEvaluation EvaluateSkillDrivers()
        {
            return default;
        }

        public AISkillDriver GrabTarget => FindDriver("GrabTarget");
        public AISkillDriver ChaseGrabTarget => FindDriver("ChaseGrabTarget");
        public AISkillDriver ChaseEnemy => FindDriver("ChaseEnemy");
        public AISkillDriver StrafeEnemy => FindDriver("StrafeEnemy");
        public AISkillDriver TetherToBody => FindDriver("TetherToBody");
        public AISkillDriver StrafeLeader => FindDriver("StrafeLeader");
        public AISkillDriver DetachLeader => FindDriver("DetachLeader");
    }
}