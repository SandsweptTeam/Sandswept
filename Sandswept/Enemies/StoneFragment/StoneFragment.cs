using System;
using System.Linq;
using RoR2.CharacterAI;
using RoR2.Navigation;
using Sandswept.Enemies.StoneFragment.States;

namespace Sandswept.Enemies.StoneFragment
{
    [ConfigSection("Enemies :: Stone Fragment")]
    public class StoneFragment : EnemyBase<StoneFragment>
    {
        [ConfigField("Director Credit Cost", "", 25)]
        public static int directorCreditCost;

        public override DirectorCardCategorySelection family => Paths.FamilyDirectorCardCategorySelection.dccsGolemFamily;
        public override MonsterCategory cat => MonsterCategory.BasicMonsters;
        public static GameObject MuzzleFlashGolem;
        public static GameObject FragmentBeam;
        public static Material matFragmentBeam;
        public static GameObject ImpactFragmentLaser;
        public override void LoadPrefabs()
        {
            prefab = Main.assets.LoadAsset<GameObject>("StoneFragmentBody.prefab");
            prefabMaster = Main.assets.LoadAsset<GameObject>("StoneFragmentMaster.prefab");

            MuzzleFlashGolem = Paths.GameObject.MuzzleflashGolem;

            FragmentBeam = Main.assets.LoadAsset<GameObject>("FragmentBeam.prefab");
            FragmentBeam.FindComponent<LineRenderer>("Line").sharedMaterial = Paths.Material.matGolemLaserIndicator;
            matFragmentBeam = Main.assets.LoadAsset<Material>("matFragmentBeam.mat");

            ImpactFragmentLaser = PrefabAPI.InstantiateClone(Paths.GameObject.ExplosionGolem, "ImpactFragmentLaser");
            // ImpactFragmentLaser.RemoveComponent<ShakeEmitter>();
            ContentAddition.AddEffect(ImpactFragmentLaser);
        }

        public override void PostCreation()
        {
            base.PostCreation();
            RegisterEnemy(prefab, prefabMaster, null, MonsterCategory.BasicMonsters, false);

            DirectorCardHolder holder = new();
            holder.Card = card;
            holder.MonsterCategory = MonsterCategory.BasicMonsters;
            holder.MonsterCategorySelectionWeight = 1;

            DirectorAPI.Helpers.AddNewMonsterToStagesWhere(holder, true, (x) => {
                string[] modded = new string[] {
                    "foggyswampdownpour",
                    "broadcastperch_wormsworms", "itbroadcastperch_wormsworms",
                    "coast_wormsworms", "itcoast_wormsworms"
                };

                if (modded.Contains(x.CustomStageName)) {
                    return true;
                }

                Stage[] stages = new DirectorAPI.Stage[] {
                    DirectorAPI.Stage.AbyssalDepths, DirectorAPI.Stage.AbyssalDepthsSimulacrum,
                    DirectorAPI.Stage.WetlandAspect,
                    DirectorAPI.Stage.TreebornColony, DirectorAPI.Stage.GoldenDieback,
                    DirectorAPI.Stage.LuridBasilica,
                    DirectorAPI.Stage.RepurposedCrater,
                    DirectorAPI.Stage.SirensCall,
                    DirectorAPI.Stage.SkyMeadow, DirectorAPI.Stage.SkyMeadowSimulacrum,
                    DirectorAPI.Stage.TenebrousQuagmire,
                    DirectorAPI.Stage.WinnowingOfPredation,
                    DirectorAPI.Stage.AbandonedAqueduct, DirectorAPI.Stage.AbandonedAqueductSimulacrum,
                    DirectorAPI.Stage.ArtifactReliquary_AbandonedAqueduct_Theme,
                    DirectorAPI.Stage.ArtifactReliquary_AphelianSanctuary_Theme,
                    DirectorAPI.Stage.PretendersPrecipice,
                };

                return stages.Contains(x.stage);
            });
        }

        public override void Modify()
        {
            base.Modify();

            master.bodyPrefab = prefab;
            body.baseNameToken.Add("Stone Fragment");

            var locator = body.GetComponent<SkillLocator>();

            ReplaceSkill(locator.primary, FireBeamSkill.instance);
            // ReplaceSkill(locator.secondary, ChimeSkill.instance);

            body.GetComponent<CharacterDeathBehavior>().deathState = new(typeof(DeathState));
            // body.GetComponent<CharacterDeathBehavior>().deathStateMachine.initialStateType = new(typeof(ArdentWispSpawn));

            LanguageAPI.Add(body.baseNameToken.Replace("_NAME", "_LORE"),
            """
            
            """);
        }

        public override void AddDirectorCard()
        {
            base.AddDirectorCard();
            card.selectionWeight = 1;
            card.spawnCard = csc;
            card.spawnDistance = DirectorCore.MonsterSpawnDistance.Standard;
        }

        public override void AddSpawnCard()
        {
            base.AddSpawnCard();
            csc.directorCreditCost = directorCreditCost;
            csc.forbiddenFlags = NodeFlags.NoCharacterSpawn;
            csc.hullSize = HullClassification.Human;
            csc.nodeGraphType = MapNodeGroup.GraphType.Air;
            csc.sendOverNetwork = true;
            csc.prefab = prefabMaster;
            csc.name = "cscStoneFragment";
        }

        public override void SetUpIDRS()
        {
            CollapseIDRS();
        }
    }
}