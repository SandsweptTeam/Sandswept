using System;
using System.Collections;
using System.Diagnostics.Contracts;
using System.Linq;
using Sandswept.Enemies.BrassBell;
using Sandswept.Enemies.BrassSpire;
using Sandswept.Survivors;
using static RoR2.MasterCatalog;

namespace Sandswept.Enemies.BrassSpire.States {
    public class Summon : BaseSkillState {
        public int bellCount = 2;
        public int contraptionCount = 1;
        private static MasterIndex BrassContraptionIndex = MasterIndex.none;
        private static MasterIndex BrassBellIndex = MasterIndex.none;

        public override void OnEnter()
        {
            base.OnEnter();

            PlayAnimation("Body", "Chime");

            if (NetworkServer.active) {
                if (BrassContraptionIndex == MasterIndex.none) {
                    BrassContraptionIndex = MasterCatalog.FindMasterIndex("BellMaster");
                }

                if (BrassBellIndex == MasterIndex.none) {
                    BrassBellIndex = MasterCatalog.FindMasterIndex("BellConstructMaster");
                }

                int bells = GetLivingMasters(BrassBellIndex);
                int contrap = GetLivingMasters(BrassContraptionIndex);

                GameObject prefab = null;

                int count = 0;

                if (bells == 0 && contrap != 0) {
                    count = 2;
                    prefab = MasterCatalog.GetMasterPrefab(BrassBellIndex);
                }
                else if (bells != 0 && contrap == 0) {
                    count = 1;
                    prefab = MasterCatalog.GetMasterPrefab(BrassContraptionIndex);
                }
                else if (bells == 0 && contrap == 0) {
                    MasterIndex index = new MasterIndex[] { BrassContraptionIndex, BrassBellIndex }.GetRandom();
                    count = index == BrassBellIndex ? 2 : 1;
                    prefab = MasterCatalog.GetMasterPrefab(index);
                }

                if (prefab == null) {
                    return;
                }

                if (count == 2) {
                    MasterSummon summon = new();
                    summon.propogateMinionOwnership = false;
                    summon.summonerBodyObject = base.gameObject;
                    summon.position = FindModelChild("Muzzle1").transform.position;
                    summon.rotation = base.transform.rotation;
                    summon.ignoreTeamMemberLimit = true;
                    summon.inventoryToCopy = base.characterBody.master.inventory;
                    summon.teamIndexOverride = base.characterBody.teamComponent.teamIndex;
                    summon.useAmbientLevel = true;
                    summon.masterPrefab = prefab;
                    summon.Perform();

                    MasterSummon summon2 = new();
                    summon2.propogateMinionOwnership = false;
                    summon2.summonerBodyObject = base.gameObject;
                    summon2.position = FindModelChild("Muzzle2").transform.position;
                    summon2.rotation = base.transform.rotation;
                    summon2.ignoreTeamMemberLimit = true;
                    summon2.inventoryToCopy = base.characterBody.master.inventory;
                    summon2.teamIndexOverride = base.characterBody.teamComponent.teamIndex;
                    summon2.useAmbientLevel = true;
                    summon2.masterPrefab = prefab;
                    summon2.Perform();
                }
                else {
                    MasterSummon summon = new();
                    summon.propogateMinionOwnership = false;
                    summon.summonerBodyObject = base.gameObject;
                    summon.position = FindModelChild("BrassPoint").transform.position;
                    summon.rotation = base.transform.rotation;
                    summon.ignoreTeamMemberLimit = true;
                    summon.inventoryToCopy = base.characterBody.master.inventory;
                    summon.teamIndexOverride = base.characterBody.teamComponent.teamIndex;
                    summon.useAmbientLevel = true;
                    summon.masterPrefab = prefab;
                    summon.Perform();
                }
            }

            outer.SetNextStateToMain();
        }

        public int GetLivingMasters(MasterIndex index) {
            int count = 0;

            foreach (CharacterMaster master in CharacterMaster.instancesList) {
                if (master.masterIndex == index && master.minionOwnership.ownerMaster == characterBody.master) {
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

        public override float Cooldown => 1f;

        public override Sprite Icon => null;
        public override bool BeginCooldownOnSkillEnd => true;
        public override bool CanceledFromSprinting => false;
        public override bool FullRestockOnAssign => false;
    }
}