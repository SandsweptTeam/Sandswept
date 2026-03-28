using System;
using System.Collections.Generic;
using System.Text;
using Sandswept.Enemies.Ivy;

namespace Sandswept.Buffs
{
    [ConfigSection("Enemies :: Ivy")]
    public class IvyBuff : BuffBase<IvyBuff>
    {
        public override string BuffName => "Ivy Grab";

        public override Color Color => Color.green;

        public override Sprite BuffIcon => null;

        public override bool CanStack => false;
        public override bool IsDebuff => false;

        [ConfigField("Ivy Grab Attack Speed Gain", "", 1f)]
        public static float ivyBuffAttackSpeedGain;

        [ConfigField("Ivy Grab Cooldown Reduction", "Decimal.", 0.5f)]
        public static float ivyBuffCooldownReduction;

        public override void Init()
        {
            base.Init();

            RecalculateStatsAPI.GetStatCoefficients += HandleSpeedBuff;
            On.RoR2.CharacterAI.BaseAI.GameObjectPassesSkillDriverFilters += OverrideAttackDistance;
        }

        public override void OnBuffApplied(CharacterBody body)
        {
            base.OnBuffApplied(body);

            if (!body.GetComponent<IvyRootEffect>()) {
                body.AddComponent<IvyRootEffect>();
            }
        }

        public override void OnBuffExpired(CharacterBody body)
        {
            base.OnBuffExpired(body);

            if (body.GetComponent<IvyRootEffect>()) {
                body.RemoveComponent<IvyRootEffect>();
            }
        }

        public class IvyRootEffect : MonoBehaviour {
            public CharacterBody body;
            public GameObject effectInstance;

            public void Start() {
                body = GetComponent<CharacterBody>();
                effectInstance = GameObject.Instantiate(Ivy.IvyGrabEffect, base.gameObject.transform);
                effectInstance.transform.position = body.corePosition;
                effectInstance.transform.localScale = Vector3.one * (2f * body.bestFitRadius);
            }

            public void OnDestroy() {
                if (effectInstance) {
                    GameObject.Destroy(effectInstance);
                }
            }
        }

        private bool OverrideAttackDistance(On.RoR2.CharacterAI.BaseAI.orig_GameObjectPassesSkillDriverFilters orig, RoR2.CharacterAI.BaseAI self, RoR2.CharacterAI.BaseAI.Target target, RoR2.CharacterAI.AISkillDriver skillDriver, out float separationSqrMagnitude)
        {
            separationSqrMagnitude = 0f;

            if (self.body && self.body.HasBuff(BuffDef)) {
                if (skillDriver.maxDistance <= 10f) {
                    return false;
                }
                else {
                    return true;
                }
            }

            return orig(self, target, skillDriver, out separationSqrMagnitude);
        }

        private void HandleSpeedBuff(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender.HasBuff(BuffDef))
            {
                args.attackSpeedMultAdd += 8f;
                args.cooldownMultAdd -= 0.99f;
            }
        }
    }
}