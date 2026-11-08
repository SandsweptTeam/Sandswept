using System;
using System.Linq;
using RoR2.Navigation;
using Sandswept.Enemies.BrassSpire.States;

namespace Sandswept.Enemies.BrassSpire
{
    [ConfigSection("Enemies :: Brass Spire")]
    public class BrassSpire : EnemyBase<BrassSpire>
    {
        public override DirectorCardCategorySelection family => null;
        public override MonsterCategory cat => MonsterCategory.BasicMonsters;
        public static GameObject BellShotProjectile;
        public static GameObject BellShotPrep;
        public static GameObject ShockwaveProjectile;

        public override void LoadPrefabs()
        {
            prefab = Main.assets.LoadAsset<GameObject>("BrassSpireBody.prefab");
            prefabMaster = Main.assets.LoadAsset<GameObject>("BrassSpireMaster.prefab");

            LanguageAPI.Add(prefab.GetComponent<CharacterBody>().baseNameToken.Replace("_NAME", "_LORE"),
            """
            
            """);

            prefab.FindComponent<MeshRenderer>("Indicator").sharedMaterial = Paths.Material.matDreadFogZoneIndicator;
            prefab.GetComponent<BuffWard>().buffDef = Buffs.BellBoost.instance.BuffDef;

            BellShotProjectile = PrefabAPI.InstantiateClone(Paths.GameObject.BellBall, "SpireBellShotProjectile");
            GameObject ghost = PrefabAPI.InstantiateClone(Paths.GameObject.BellBallGhost, "SpireBellShotGhost");
            ghost.GetComponent<ProjectileGhostController>().inheritScaleFromProjectile = true;

            BellShotProjectile.EditComponent<ProjectileController>((x) => {
                x.ghostPrefabAddress = null;
                x.ghostPrefab = ghost;
            });

            BellShotPrep = PrefabAPI.InstantiateClone(Paths.GameObject.PreppedBellBall, "SpireBellShotPrep");

            BellShotProjectile.transform.localScale *= 1.2f;
            ghost.transform.localScale *= 1.2f;
            BellShotPrep.transform.localScale *= 1.2f;

            ContentAddition.AddProjectile(BellShotProjectile);

            ShockwaveProjectile = PrefabAPI.InstantiateClone(Paths.GameObject.SlamRing, "SpireShockwave", false);
            var sGhost = PrefabAPI.InstantiateClone(Paths.GameObject.RingOfFireGhost, "SpireShockwaveGhost");

            ShockwaveProjectile.EditComponent<ProjectileController>((x) => {
                x.ghostPrefabAddress = null;
                x.ghostPrefab = sGhost;
            });

            sGhost.FindComponent<MeshRenderer>("Ring").sharedMaterial = Paths.Material.matClayGrenadierShockwave;
        }

        public override void PostCreation()
        {
            base.PostCreation();
            RegisterEnemy(prefab, prefabMaster, null, MonsterCategory.BasicMonsters, false);
        }

        public override void AddDirectorCard()
        {
            base.AddDirectorCard();
        }

        public override void AddSpawnCard()
        {
            base.AddSpawnCard();
        }

        public override void Modify()
        {
            base.Modify();

            master.bodyPrefab = prefab;

            body.baseNameToken.Add("Brass Spire");

            var locator = body.GetComponent<SkillLocator>();

            ReplaceSkill(locator.primary, BarrageSkill.instance);
            ReplaceSkill(locator.secondary, Paths.SkillDef.CommandoBodyBarrage);
            ReplaceSkill(locator.utility, ShockwaveSkill.instance);
            ReplaceSkill(locator.special, SummonSkill.instance);
        }

        public override void SetUpIDRS()
        {
            CollapseIDRS();
        }
    }
}