using System;
using System.Linq;
using RoR2.Navigation;

namespace Sandswept.Enemies.TinyImp
{
    [ConfigSection("Enemies :: Tiny Imp")]
    public class TinyImp : EnemyBase<TinyImp>
    {
        public override DirectorCardCategorySelection family => null;
        public override MonsterCategory cat => MonsterCategory.BasicMonsters;

        public override void LoadPrefabs()
        {
            prefab = Main.assets.LoadAsset<GameObject>("SSTinyImpBody.prefab");
            prefabMaster = Main.assets.LoadAsset<GameObject>("SSTinyImpMaster.prefab");

            LanguageAPI.Add(prefab.GetComponent<CharacterBody>().baseNameToken.Replace("_NAME", "_LORE"),
            """
            
            """);
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

            body.baseNameToken.Add("Tiny Imp");
        }

        public override void SetUpIDRS()
        {
            CollapseIDRS();
        }
    }
}