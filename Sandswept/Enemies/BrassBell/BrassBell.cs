using System;
using System.Linq;
using RoR2.CharacterAI;
using RoR2.Navigation;
using Sandswept.Enemies.BrassBell.States;

namespace Sandswept.Enemies.BrassBell
{
    [ConfigSection("Enemies :: Brass Bell")]
    public class BrassBell : EnemyBase<BrassBell>
    {
        [ConfigField("Director Credit Cost", "", 20)]
        public static int directorCreditCost;

        public override DirectorCardCategorySelection family => null;
        public override MonsterCategory cat => MonsterCategory.BasicMonsters;
        public static GameObject BellShotProjectile;
        public static GameObject BellShotPrep;
        public static GameObject BellChimeEffect;

        public override void LoadPrefabs()
        {
            prefab = Main.assets.LoadAsset<GameObject>("BellConstructBody.prefab");
            prefabMaster = Main.assets.LoadAsset<GameObject>("BellConstructMaster.prefab");

            BellShotProjectile = PrefabAPI.InstantiateClone(Paths.GameObject.BellBall, "BellShotProjectile");
            GameObject ghost = PrefabAPI.InstantiateClone(Paths.GameObject.BellBallGhost, "BellShotGhost");
            ghost.GetComponent<ProjectileGhostController>().inheritScaleFromProjectile = true;

            BellShotProjectile.EditComponent<ProjectileController>((x) => {
                x.ghostPrefabAddress = null;
                x.ghostPrefab = ghost;
            });

            BellShotPrep = PrefabAPI.InstantiateClone(Paths.GameObject.PreppedBellBall, "BellShotPrep");

            BellShotProjectile.transform.localScale *= 0.5f;
            ghost.transform.localScale *= 0.5f;
            BellShotPrep.transform.localScale *= 0.5f;

            ContentAddition.AddProjectile(BellShotProjectile);

            BellChimeEffect = Main.assets.LoadAsset<GameObject>("BrassChimeEffect.prefab");
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
            body.baseNameToken.Add("Brass Bell");

            var locator = body.GetComponent<SkillLocator>();

            ReplaceSkill(locator.primary, BarrageSkill.instance);
            ReplaceSkill(locator.secondary, ChimeSkill.instance);

            body.GetComponent<CharacterDeathBehavior>().deathState = new(typeof(DeathState));
            // body.GetComponent<CharacterDeathBehavior>().deathStateMachine.initialStateType = new(typeof(ArdentWispSpawn));

            LanguageAPI.Add(body.baseNameToken.Replace("_NAME", "_LORE"),
            """
            Incident Report: 720AYT-701 "Grand Symphony"

            [6th of November, 2057 at 10:45pm] The UES Shimmer Mark sends out a daily survey report, noting an unknown vessel. Witnesses described the object to be 'around the fifth of the size of the nearest moon"
            [6th of November, 2057 at 10:47pm] The UES Manifest Destiny is alerted of the UES Shimmer Mark's anomaly mentioned in their survey report, the Manifest Destiny near immediately re-routed to the object's predicted location.
            [10th of November, 2057 at 4:22am] The UES Manifest Destiny arrives at the anomalous object in the Versanto System
            [10th of November, 2057 at 4:40am] The Captain of the UES Manifest Destiny writes in his personal ship log that the ship started "Shaking violently upon getting close"
            [10th of November, 2057 at 6:23am] Anomaly is deemed to be located within the upper atmosphere of Versanto II due to scout drones recording sound coming from the Anomaly planetside.
            [10th of November, 2057 at 7:44am] Anomaly is seen releasing a stream of sub-anomalies from a port on its bottom.
            [10th of November, 2057 at 10:25am] Scout drones go dark after recording two and a half hours of footage. Reported sub-anomalies were recorded to be instruments. Various percussive and string instruments were spotted, however there were no brass, woodwind, or piano instruments.
            [10th of November, 2057 at 11:41am] Crewmates aboard the UES Manifest Destiny report seeing sub-anomalies drifting towards the vessel, the captain dismisses these claims.
            [10th of November, 2057 at 12:00am] Automatic trash disposal activates, allowing in one sub-anomaly, dubbed a 'Brass Bell'
            [10th of November, 2057 at 3:53am] Crewmate spots and destroys the Brass Bell. The captain quickly calls for pilots to retreat from Versanto II
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
            csc.name = "cscBrassBell";
        }

        public override void SetUpIDRS()
        {
            CollapseIDRS();
        }
    }
}