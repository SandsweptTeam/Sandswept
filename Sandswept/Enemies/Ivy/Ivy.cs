using System;
using RoR2.Navigation;
using RoR2.Orbs;

namespace Sandswept.Enemies.Ivy
{
    [ConfigSection("Enemies :: Gamma Construct")]
    public class Ivy : EnemyBase<Ivy>
    {
        [ConfigField("Director Credit Cost", "", 115)]
        public static int directorCreditCost;

        public override DirectorCardCategorySelection family => null;
        public override MonsterCategory cat => MonsterCategory.Minibosses;
        public static GameObject IvyHeadBody;
        public static GameObject IvyHeadMaster;
        public static GameObject IvyGrabEffect;

        // TODO
        // better grab vfx
        // sfx
        // figure out nodegraph issue (rn the hacky workaround is disabling nodegraph for those drivers but thats obv dumb and leads to the enemies being carried through walls)
        // figure out why the head isnt perfectly in sync with the neck end when weight is moving from 0 <-> 1
        // add caustic spit attack
        // make held enemy get thrown after 10s or so
        // tune StriderLegController params
        // give it hitboxes lmfao

        public override void LoadPrefabs()
        {
            prefab = Main.assets.LoadAsset<GameObject>("IvyBody.prefab");
            prefabMaster = Main.assets.LoadAsset<GameObject>("IvyMaster.prefab");
            LanguageAPI.Add(prefab.GetComponent<CharacterBody>().baseNameToken.Replace("_NAME", "_LORE"),
            """
            tbd
            </style>
            """);

            IvyHeadBody = PrefabAPI.InstantiateClone(Paths.GameObject.WispBody, "IvyHeadBody");
            IvyHeadMaster = Main.assets.LoadAsset<GameObject>("IvyHeadMaster.prefab");
            IvyHeadBody.AddComponent<IvyBodyMarker>();
            IvyHeadBody.EditComponent<CharacterBody>((x) => {
                x.baseMoveSpeed = 20;
                x.baseAcceleration = 140;
            });
            IvyHeadBody.GetComponent<ModelLocator>().modelBaseTransform.gameObject.SetActive(false);
            IvyHeadBody.layer = LayerIndex.noCollision.intVal;
            IvyHeadMaster.GetComponent<CharacterMaster>().bodyPrefab = IvyHeadBody;

            ContentAddition.AddBody(IvyHeadBody);
            ContentAddition.AddMaster(IvyHeadMaster);

            SkillLocator loc = prefab.GetComponent<SkillLocator>();
            SkillLocator loc2 = IvyHeadBody.GetComponent<SkillLocator>();

            ReplaceSkill(loc.secondary, BeginSearchSkill.instance);
            ReplaceSkill(loc.utility, DeployHeadSkill.instance);
            ReplaceSkill(loc2.primary, GrabTargetSkill.instance);
            
            SetUpVFX();
        }

        public void SetUpVFX()
        {
            IvyGrabEffect = PrefabAPI.InstantiateClone(Paths.GameObject.EntangleOrbEffect, "IvyGrabEffect");
            IvyGrabEffect.GetComponentInChildren<LineRenderer>().enabled = false;
            IvyGrabEffect.GetComponentInChildren<ParticleSystemRenderer>().material = Main.assets.LoadAsset<Material>("matIvy.mat");
            IvyGrabEffect.RemoveComponent<OrbEffect>();
            IvyGrabEffect.RemoveComponent<EffectComponent>();
        }

        public class IvyBodyMarker : MonoBehaviour {
            bool foundOwner = false;
            public CharacterBody cb;
            public ModelLocator loc;
            public void Start() {
                cb = GetComponent<CharacterBody>();
                loc = GetComponent<ModelLocator>();
                loc.modelBaseTransform.gameObject.SetActive(false);
            }
            public void FixedUpdate() {
                if (loc && loc.modelBaseTransform && loc.modelBaseTransform.gameObject.activeInHierarchy) {
                    loc.modelBaseTransform.gameObject.SetActive(false);
                }

                if (foundOwner) return;

                if (cb && cb.master && cb.master.minionOwnership && cb.master.minionOwnership.ownerMaster) {
                    CharacterBody body = cb.master.minionOwnership.ownerMaster.GetBody();

                    if (body) {
                        (EntityStateMachine.FindByCustomName(body.gameObject, "Body").state as IvyMainState).IvyHeadBody = cb;
                        foundOwner = true;
                    }
                }
            }
        }

        public override void PostCreation()
        {
            base.PostCreation();

            List<Stage> stages = new List<DirectorAPI.Stage> {
                Stage.SkyMeadow,
                Stage.SkyMeadowSimulacrum,
                DirectorAPI.Stage.SulfurPools,
                DirectorAPI.Stage.TreebornColony,
                DirectorAPI.Stage.GoldenDieback,
                DirectorAPI.Stage.ArtifactReliquary_AphelianSanctuary_Theme,
                DirectorAPI.Stage.DisturbedImpact,
                DirectorAPI.Stage.ViscousFalls,
                DirectorAPI.Stage.ScorchedAcres
            };

            RegisterEnemy(prefab, prefabMaster, stages, MonsterCategory.Minibosses);
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
            csc.hullSize = HullClassification.Golem;
            csc.nodeGraphType = MapNodeGroup.GraphType.Ground;
            csc.sendOverNetwork = true;
            csc.prefab = prefabMaster;
            csc.name = "cscIvy";
        }

        public override void Modify()
        {
            base.Modify();

            master.bodyPrefab = prefab;

            body.baseNameToken.Add("Ivy");

            SkillLocator loc = body.GetComponent<SkillLocator>();

            // ReplaceSkill(loc.primary, FireBeamSkill.instance.skillDef);
            // ReplaceSkill(loc.secondary, FireTwinBeamSkill.instance.skillDef);

            // prefab.GetComponent<CharacterDeathBehavior>().deathState = new(typeof(DeathState));
            // EntityStateMachine.FindByCustomName(prefab, "Body").initialStateType = new(typeof(SpawnState));
        }
    }
}