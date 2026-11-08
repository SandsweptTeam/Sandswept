using System;
using System.Collections;
using System.Diagnostics.Contracts;
using System.Linq;
using Sandswept.Enemies.BrassSpire;
using Sandswept.Survivors;
using static RoR2.MasterCatalog;

namespace Sandswept.Enemies.BrassChoir.States {
    public class Summon : BaseSkillState {
        public int spireCount = 2;
        public int totalSpires = 4;
        private static MasterIndex BrassSpireIndex = MasterIndex.none;

        public override void OnEnter()
        {
            base.OnEnter();

            if (NetworkServer.active) {
                base.characterBody.StartCoroutine(SummonSpires());
                BrassChoir.ReplicateToSpires(base.characterBody, SkillSlot.Utility);
            }

            outer.SetNextStateToMain();
        }

        public IEnumerator SummonSpires() {
            List<Vector3> positions = MiscUtils.GetSafePositionsWithinDistance(base.transform.position, 40f).ToList();
            for (int i = 0; i < spireCount; i++)
            {
                yield return new WaitForSeconds(0.2f);
                if (positions.Count <= 1 || GetLivingSpires() >= totalSpires)
                {
                    continue;
                }

                MasterSummon summon = new();
                summon.summonerBodyObject = base.gameObject;
                summon.position = positions.GetRandom();
                positions.Remove(summon.position);
                summon.rotation = base.transform.rotation;
                summon.ignoreTeamMemberLimit = true;
                summon.inventoryToCopy = base.characterBody.master.inventory;
                summon.teamIndexOverride = base.characterBody.teamComponent.teamIndex;
                summon.useAmbientLevel = true;
                summon.masterPrefab = BrassSpire.BrassSpire.Instance.prefabMaster;
                summon.Perform();
            }
        }

        public int GetLivingSpires() {
            if (BrassSpireIndex == MasterIndex.none) {
                BrassSpireIndex = MasterCatalog.FindMasterIndex("BrassSpireMaster");
            }

            int count = 0;

            foreach (CharacterMaster master in CharacterMaster.instancesList) {
                if (master.masterIndex == BrassSpireIndex) {
                    count++;
                }
            }

            return count;
        }
    }

    public class SummonSkill : SkillBase<SummonSkill> {
        public override string Name => "";

        public override string Description => "";

        public override Type ActivationStateType => typeof(Summon);

        public override string ActivationMachineName => "Weapon";

        public override float Cooldown => 12f;

        public override Sprite Icon => null;
        public override bool BeginCooldownOnSkillEnd => true;
        public override bool CanceledFromSprinting => false;
        public override bool FullRestockOnAssign => false;
    }
}