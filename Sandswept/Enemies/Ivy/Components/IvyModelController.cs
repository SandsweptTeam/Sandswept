using System;
using Generics.Dynamics;
using RoR2.ConVar;
using UnityEngine.Animations;

namespace Sandswept.Enemies.Ivy {
    public class IvyModelController : MonoBehaviour {
        public Transform VineRoot;
        public int RecursiveDepth;
        public Transform IKTarget;
        public Transform head;
        public float ArcHeight = 15f;
        public bool headActive = false;
        public bool refreshStats = false;
        public float headLerp = 0.3f;
        internal float interpolationTime = 1.1f;
        internal float interpolationStopwatch = 0f;
        private bool headWasActive = false;
        private bool switching = false;
        private float weight = 0f;
        private Transform[] transforms;
        //
        private Animator anim;
        public string BodyState;
        public string HeadState;
        public void Start() {
            Transform lastTransform = VineRoot;
            List<Transform> all = new();
            for (int i = 0; i < RecursiveDepth; i++) {
                if (lastTransform && lastTransform.childCount > 0) {
                    lastTransform = lastTransform.GetChild(0);
                }
                else {
                    continue;
                }

                all.Add(lastTransform);
            }

            transforms = all.ToArray();

            //
            anim = GetComponent<Animator>();
        }

        public void Update() {
            try {
                BodyState = anim.GetCurrentAnimatorClipInfo(anim.GetLayerIndex("Body"))[0].clip.name;
            }
            catch {
                BodyState = "null";
            }
            // HeadState = anim.GetCurrentAnimatorClipInfo(anim.GetLayerIndex("Override, Head"))[0].clip.name;

            if (headActive != headWasActive) {
                headWasActive = headActive;
                interpolationStopwatch = 0f;
                switching = true;
            }


            if (switching && headActive) {
                interpolationStopwatch += Time.deltaTime;
                weight = Mathf.Clamp01(interpolationStopwatch / interpolationTime);

                if (interpolationStopwatch >= interpolationTime) {
                    switching = false;
                    interpolationStopwatch = 0f;
                }
            }

            if (switching && !headActive) {
                interpolationStopwatch += Time.deltaTime;
                weight = Mathf.Clamp01(1f - (interpolationStopwatch / interpolationTime));

                if (interpolationStopwatch >= interpolationTime) {
                    interpolationStopwatch = 0f;
                    switching = false;
                }
            }
        }

        public void LateUpdate() {
            if (weight > 0f) {
                BeizerCurve curve = new(transforms[0].position, MiscUtils.MidpointAtHeight(transforms[0].position, IKTarget.position, ArcHeight), IKTarget.position, 200);

                for (int i = 0; i < transforms.Length; i++) {
                    float time = (float)i / (float)transforms.Length;
                    Vector3 point = curve.GetBeizerPoint(time);
                    Vector3 forward = curve.GetRotationAlongCurve(time, 0.05f);

                    transforms[i].position = Vector3.Lerp(transforms[i].position, point, Mathf.Lerp(0f, weight, Mathf.Clamp01(i / 8f)));
                    transforms[i].up = Vector3.Lerp(transforms[i].up, forward, Mathf.Lerp(0f, weight, Mathf.Clamp01(i / 8f)));
                }

                head.transform.up = Vector3.Lerp(head.transform.up, -transforms[transforms.Length - 1].up, weight);
                head.transform.position = transforms[transforms.Length - 2].position;
            }
        }
    }
}