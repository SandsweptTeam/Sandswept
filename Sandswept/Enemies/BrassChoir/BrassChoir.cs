using System;
using System.Linq;
using RoR2.CharacterAI;
using RoR2.Navigation;
using Sandswept.Enemies.BrassChoir.States;
using static RoR2.MasterCatalog;

namespace Sandswept.Enemies.BrassChoir
{
    [ConfigSection("Enemies :: Brass Choir")]
    public class BrassChoir : EnemyBase<BrassChoir>
    {
        [ConfigField("Director Credit Cost", "", 600)]
        public static int directorCreditCost;
        public override DirectorCardCategorySelection family => null;
        public override MonsterCategory cat => MonsterCategory.BasicMonsters;
        public static GameObject BellShotProjectile;
        public static GameObject BellShotPrep;
        private static MasterIndex BrassSpireIndex = MasterIndex.none;
        public static GameObject BellTether;

        public override void LoadPrefabs()
        {
            prefab = Main.assets.LoadAsset<GameObject>("BrassChoirBody.prefab");
            prefabMaster = Main.assets.LoadAsset<GameObject>("BrassChoirMaster.prefab");

            prefab.GetComponent<CameraTargetParams>().cameraParams = Paths.CharacterCameraParams.ccpStandardHuge;

            LanguageAPI.Add(prefab.GetComponent<CharacterBody>().baseNameToken.Replace("_NAME", "_LORE"),
            """
            
            """);

            BellShotProjectile = PrefabAPI.InstantiateClone(Paths.GameObject.BellBall, "ChoirBellShotProjectile");
            GameObject ghost = PrefabAPI.InstantiateClone(Paths.GameObject.BellBallGhost, "ChoirBellShotGhost");
            ghost.GetComponent<ProjectileGhostController>().inheritScaleFromProjectile = true;

            BellShotProjectile.EditComponent<ProjectileController>((x) => {
                x.ghostPrefabAddress = null;
                x.ghostPrefab = ghost;
            });

            BellShotPrep = PrefabAPI.InstantiateClone(Paths.GameObject.PreppedBellBall, "ChoirBellShotPrep");

            BellShotProjectile.transform.localScale *= 2.5f;
            ghost.transform.localScale *= 2.5f;
            BellShotPrep.transform.localScale *= 2.5f;

            ContentAddition.AddProjectile(BellShotProjectile);

            On.RoR2.ProjectileCatalog.Init += PrepAllBrassShots;

            BellTether = Main.assets.LoadAsset<GameObject>("ChoirTetherEffect.prefab");
        }

        private static string[] AllBrassShots = {
            "BellBall", "ChoirBellShotProjectile", "BellShotProjectile", "SpireBellShotProjectile"
        };
        private void PrepAllBrassShots(On.RoR2.ProjectileCatalog.orig_Init orig)
        {
            orig();

            var effect = Paths.GameObject.HermitCrabBombExplosion;

            foreach (string name in AllBrassShots) {
                GameObject prefab = ProjectileCatalog.GetProjectilePrefab(ProjectileCatalog.FindProjectileIndex(name));

                var explo = prefab.AddComponent<ProjectileExplosion>((x) => {
                    x.blastRadius = 4f * prefab.transform.localScale.x;
                    x.blastDamageCoefficient = 1f;
                    x.falloffModel = BlastAttack.FalloffModel.SweetSpot;
                    x.blastProcCoefficient = 1f;
                    x.explosionEffect = effect;
                });

                prefab.AddComponent<BellShotController>().explosion = explo;
            }
        }

        public class BellShotController : MonoBehaviour {
            public static List<BellShotController> instancesList = new();
            public ProjectileExplosion explosion;
            private GameObject radius;
            private float stopwatch = 0f;
            private bool isExploding;
            private float exploTime = 1.2f;
            private float radiusTime = 0.8f;

            public void OnEnable() {
                instancesList.Add(this);
            }

            public void OnDisable() {
                instancesList.Remove(this);
            }

            public void Start() {
                explosion.enabled = false;

                radius = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                radius.RemoveComponent<SphereCollider>();
                radius.GetComponent<MeshRenderer>().sharedMaterial = Paths.Material.matTeamAreaIndicatorIntersectionMonster;
                radius.transform.localScale = Vector3.zero;

                foreach (CharacterMaster master in CharacterMaster.instancesList) {
                    if (master.masterIndex == BrassChoir.Instance.master.masterIndex) {
                        GetComponent<ProjectileSimple>().lifetime = 15f;
                        if (TryGetComponent<ProjectileImpactExplosion>(out var exp)) {
                            exp.lifetime = 15f;
                            exp.lifetimeAfterImpact = 15f;
                        }

                        return;
                    }
                }
            }

            public void Explode() {
                isExploding = true;
                explosion.enabled = true;
            }

            public void Update() {
                radius.transform.position = base.transform.position;

                if (isExploding) {
                    stopwatch += Time.deltaTime;
                    float rad = explosion.blastRadius * Mathf.Clamp01(stopwatch / radiusTime);

                    radius.transform.localScale = Vector3.one * rad * 2f;

                    if (stopwatch >= exploTime && NetworkServer.active) {
                        stopwatch = 0f;
                        isExploding = false;

                        explosion.Detonate();
                    }
                }
            }

            public void OnDestroy() {
                if (radius) {
                    GameObject.Destroy(radius);
                }
            }
        }

        public static void ReplicateToSpires(CharacterBody body, SkillSlot slot) {
            if (BrassSpireIndex == MasterIndex.none) {
                BrassSpireIndex = MasterCatalog.FindMasterIndex("BrassSpireMaster");
            }

            BaseAI ai = body.master.GetComponent<BaseAI>();

            foreach (CharacterMaster master in CharacterMaster.instancesList) {
                if (master.masterIndex == BrassSpireIndex) {
                    CharacterBody spire = master.GetBody();

                    if (ai) {
                        master.GetComponent<BaseAI>().currentEnemy.gameObject = ai.currentEnemy.gameObject;
                    }

                    if (spire) {
                        switch (slot) {
                            case SkillSlot.Primary:
                                spire.skillLocator.primary.ExecuteIfReady();
                                break;
                            case SkillSlot.Secondary:
                                spire.skillLocator.secondary.ExecuteIfReady();
                                break;
                            case SkillSlot.Utility:
                                spire.skillLocator.utility.ExecuteIfReady();
                                break;
                            case SkillSlot.Special:
                                spire.skillLocator.special.ExecuteIfReady();
                                break;
                        }
                    }
                }
            }
        }

        public override void PostCreation()
        {
            base.PostCreation();
            RegisterEnemy(prefab, prefabMaster, null, MonsterCategory.Champions, false);

            DirectorCardHolder holder = new();
            holder.Card = card;
            holder.MonsterCategory = MonsterCategory.Champions;
            holder.MonsterCategorySelectionWeight = 10;

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
            csc.hullSize = HullClassification.BeetleQueen;
            csc.nodeGraphType = MapNodeGroup.GraphType.Ground;
            csc.sendOverNetwork = true;
            csc.prefab = prefabMaster;
            csc.name = "cscBrassChoir";
        }

        public override void Modify()
        {
            base.Modify();

            master.bodyPrefab = prefab;

            body.baseNameToken.Add("Brass Choir");
            body.subtitleNameToken.Add("Harbinger of Symphony");

            var locator = body.GetComponent<SkillLocator>();

            ReplaceSkill(locator.primary, BarrageSkill.instance);
            ReplaceSkill(locator.secondary, DetonateSkill.instance);
            ReplaceSkill(locator.utility, SummonSkill.instance);
            ReplaceSkill(locator.special, BellAuraSkill.instance);

            body.GetComponent<CharacterDeathBehavior>().deathState = new(typeof(DeathState));
            body.GetComponent<CharacterDeathBehavior>().deathStateMachine.initialStateType = new(typeof(SpawnState));
        }

        public override void SetUpIDRS()
        {
            CollapseIDRS();
        }
    }
}