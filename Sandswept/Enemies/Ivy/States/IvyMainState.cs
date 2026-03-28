using System;
using RoR2.CharacterAI;
using Sandswept.Buffs;

namespace Sandswept.Enemies.Ivy {
    public class IvyMainState : GenericCharacterMain {
        public CharacterBody IvyHeadBody;
        public IvyModelController controller;
        public VehicleSeat seat;
        public BaseAI ai;
        public override void OnEnter()
        {
            base.OnEnter();

            if (NetworkServer.active) {
                MasterSummon summon = new();
                summon.masterPrefab = Ivy.IvyHeadMaster;
                summon.position = base.transform.position;
                summon.summonerBodyObject = base.gameObject;
                summon.Perform();
            }

            controller = GetModelTransform().GetComponent<IvyModelController>();
            seat = GetComponent<VehicleSeat>();

            seat.onPassengerEnter += OnPassengerEnter;
            seat.onPassengerExit += OnPassengerExit;

            skillLocator.secondary.DeductStock(1);
            skillLocator.secondary.rechargeStopwatch = 10f;

            controller.IKTarget.parent = null;

            ai = base.characterBody.master.aiComponents[0];
        }

        public override void Update()
        {
            base.Update();

            if (IvyHeadBody != null) {
                controller.IKTarget.position = Vector3.MoveTowards(controller.IKTarget.position, IvyHeadBody.corePosition, 20f * Time.fixedDeltaTime);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (controller.headActive && ai && !ai.customTarget.gameObject) {
                controller.headActive = false;
            }
        }

        private void OnPassengerEnter(GameObject passenger)
        {
            CharacterBody body = passenger.GetComponent<CharacterBody>();

            if (body) {
                body.AddBuff(IvyBuff.instance.BuffDef);
            }
        }        

        private void OnPassengerExit(GameObject passenger)
        {
            skillLocator.secondary.SetBlockedCooldownSkillState(false);
            controller.headActive = false;

            CharacterBody body = passenger.GetComponent<CharacterBody>();

            if (body) {
                body.RemoveBuff(IvyBuff.instance.BuffDef);
            }
        }

        public override void OnExit()
        {
            base.OnExit();

            if (IvyHeadBody != null && NetworkServer.active) {
                IvyHeadBody.healthComponent.Suicide();
            }

            seat.onPassengerEnter -= OnPassengerEnter;
            seat.onPassengerExit -= OnPassengerExit;
        }
    }
}