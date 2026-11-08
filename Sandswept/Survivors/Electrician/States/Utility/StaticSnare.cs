using System;
using EntityStates.Engi.EngiWeapon;
using R2API.Networking.Interfaces;

namespace Sandswept.Survivors.Electrician.States
{
    public class StaticSnare : PlaceTurret {
        public override void OnEnter()
        {
            base.blueprintPrefab = Paths.GameObject.EngiTurretBlueprints;
            base.OnEnter();
        }

        public override void FixedUpdate()
        {
            base.fixedAge += GetDeltaTime();

            if (!base.isAuthority)
            {
                return;
            }

            entryCountdown -= GetDeltaTime();

            if (exitPending)
            {
                exitCountdown -= GetDeltaTime();

                if (exitCountdown <= 0f)
                {
                    outer.SetNextStateToMain();
                }
            }
            else
            {
                if (!base.inputBank || !(entryCountdown <= 0f))
                {
                    return;
                }

                if ((base.inputBank.skill1.down || base.inputBank.skill3.justPressed) && currentPlacementInfo.ok)
                {
                    if (base.characterBody)
                    {
                        FireProjectileInfo info = new();
                        info.position = currentPlacementInfo.position;
                        info.damage = base.damageStat;
                        info.owner = base.gameObject;
                        info.projectilePrefab = VFX.StaticSnare.staticSnareDefault;
                        info.rotation = Quaternion.identity;

                        ProjectileManager.instance.FireProjectile(info);

                        if (base.skillLocator)
                        {
                            GenericSkill skill = base.skillLocator.GetSkill(SkillSlot.Utility);
                            if (skill)
                            {
                                skill.DeductStock(1);
                            }
                        }
                    }

                    Util.PlaySound(placeSoundString, base.gameObject);
                    DestroyBlueprints();
                    exitPending = true;
                }

                if (base.inputBank.skill2.justPressed)
                {
                    DestroyBlueprints();
                    exitPending = true;
                }
            }
        }
    }
}