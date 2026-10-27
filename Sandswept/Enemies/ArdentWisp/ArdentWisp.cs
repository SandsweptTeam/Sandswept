using System;
using RoR2.CharacterAI;
using RoR2.Navigation;
using Sandswept.Enemies.ArdentWisp;
using Sandswept.Enemies.ArdentWisp.States;

namespace Sandswept.Enemies.ArdentWisp
{
    [ConfigSection("Enemies :: Ardent Wisp")]
    public class ArdentWisp : EnemyBase<ArdentWisp>
    {
        [ConfigField("Director Credit Cost", "", 250)]
        public static int directorCreditCost;

        public override DirectorCardCategorySelection family => Paths.FamilyDirectorCardCategorySelection.dccsWispFamily;
        public override MonsterCategory cat => MonsterCategory.Minibosses;

        public static GameObject ArdentChargeLine;
        public static GameObject ArdentExplosion;
        public static GameObject ArdentFireball;
        public static GameObject ArdentDeathEffect;
        public static Material IndicatorArdentWisp;
        public static GameObject WispBarrageShot;

        public override void LoadPrefabs()
        {
            prefab = Main.assets.LoadAsset<GameObject>("ArdentWispBody.prefab");
            prefabMaster = Main.assets.LoadAsset<GameObject>("ArdentWispMaster.prefab");

            IndicatorArdentWisp = Object.Instantiate(Paths.Material.matNullBombAreaIndicator);
            IndicatorArdentWisp.SetFloat("_RimStrength", 0f);
            IndicatorArdentWisp.SetFloat("_InvFade", 1.1416f);
            IndicatorArdentWisp.SetFloat("_SoftPower", 2.6864f);
            IndicatorArdentWisp.SetFloat("_Boost", 1.611f);
            IndicatorArdentWisp.SetFloat("_RimPower", 10.6f);
            IndicatorArdentWisp.SetFloat("_AlphaBoost", 2.66f);
            IndicatorArdentWisp.SetFloat("_IntersectionStrength", 0.3f);
            IndicatorArdentWisp.SetTexture("_RemapTex", Paths.Texture2D.texRampAncientWisp);
            Object.DontDestroyOnLoad(IndicatorArdentWisp);

            ArdentChargeLine = Main.assets.LoadAsset<GameObject>("ArdentStrikeEffect.prefab");
            ArdentChargeLine.AddComponent<VFXAttributes>((x) =>
            {
                x.DoNotPool = true;
                x.DoNotCullPool = true;
            });
            ArdentChargeLine.GetComponent<ArdentFlareCharge>().radius.sharedMaterial = IndicatorArdentWisp;

            ContentAddition.AddEffect(ArdentChargeLine);

            ArdentExplosion = Main.assets.LoadAsset<GameObject>("ArdentExplosionEffect.prefab");
            ArdentExplosion.GetComponent<VFXAttributes>().DoNotPool = true;

            ContentAddition.AddEffect(ArdentExplosion);

            ArdentFireball = Main.assets.LoadAsset<GameObject>("ArdentFlameProjectile.prefab");
            ContentAddition.AddEffect(ArdentFireball);

            ArdentDeathEffect = Main.assets.LoadAsset<GameObject>("ArdentWispDeath.prefab");
            ContentAddition.AddEffect(ArdentDeathEffect);

            WispBarrageShot = PrefabAPI.InstantiateClone(Paths.GameObject.GravekeeperTrackingFireball, "ArdentWispShot");
            WispBarrageShot.EditComponent<VelocityRandomOnStart>((x) => {
                x.minSpeed = 10f;
                x.maxSpeed = 15f;
            });
            GameObject ghost = PrefabAPI.InstantiateClone(Paths.GameObject.GravekeeperTrackingFireballGhost, "ArdentWispShotGhost");
            ghost.transform.Find("Spinner").gameObject.SetActive(true);
            ghost.transform.Find("Flames").GetComponent<ParticleSystemRenderer>().sharedMaterial = Main.assets.LoadAsset<Material>("matArdentFlame.mat");
            WispBarrageShot.GetComponent<ProjectileController>().ghostPrefab = ghost;
            WispBarrageShot.GetComponent<ProjectileController>().ghostPrefabAddress = null;
            ContentAddition.AddBody(WispBarrageShot);
            ContentAddition.AddProjectile(WispBarrageShot);
        }

        public override void PostCreation()
        {
            base.PostCreation();
            RegisterEnemy(prefab, prefabMaster, null, MonsterCategory.BasicMonsters, false);

            string[] stages = new string[] {
                "skymeadow", "helminthroost", "dampcavesimple", "rootjungle", "sanctum", "meridian", "itobservatory_wormsworms"
            };

            foreach (string stage in stages) {
                DirectorAPI.Helpers.AddNewMonsterToStage(card, MonsterCategory.Minibosses, Stage.Custom, stage);
            }
        }

        public override void Modify()
        {
            base.Modify();

            master.bodyPrefab = prefab;
            body.baseNameToken.Add("Ardent Wisp");

            var locator = body.GetComponent<SkillLocator>();

            ReplaceSkill(locator.primary, States.WispBarrageSkill.instance.skillDef);
            ReplaceSkill(locator.secondary, States.CarpetFireSkill.instance.skillDef);

            body.GetComponent<CharacterDeathBehavior>().deathState = new(typeof(ArdentWispDeath));
            body.GetComponent<CharacterDeathBehavior>().deathStateMachine.initialStateType = new(typeof(ArdentWispSpawn));

            LanguageAPI.Add(body.baseNameToken.Replace("_NAME", "_LORE"),
            """
            A LAST RESORT, FOR OUR GARDEN MOST PRECIOUS 

            START WITH A MASK, RIDDLED WITH HOLES. IT'S BASE OF YOUNGTREE, FOR SPEED, EFFICENCY
            A SCOOP OF CHARRED GLASS, THIS IS FOR SHARPNESS, ANGER
            A COATING OF "GAZOLLON" FROM THE DEMON'S BUCKET, FOR HEAT, POWER
            A MINISCULE PINCH OF [powder], FOR A WILL TO FOLLOW WITHOUT COMPASSION

            FOR A TIME SO DIRE, SACRIFICES MUST BE MADE
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
            csc.name = "cscArdentWisp";
        }

        public override void SetUpIDRS()
        {
            CollapseIDRS();
        }
    }
}