using System;
using System.Collections;
using System.Linq;
using RoR2BepInExPack.Utilities;

namespace Sandswept.Survivors.Electrician {
    public class ElecZiplineController : MonoBehaviour {
        internal ZiprailController ziprail;
        private ProjectileController self;
        internal static FixedConditionalWeakTable<CharacterBody, List<ElecZiplineController>> ActivePylons = new();
        private CharacterBody body;
        private bool init = false;
        private float initDelay = 2f;
        public Transform start;
        public Transform end;
        private GameObject[] oldPoints;
        internal ElecZiplineController next;
        internal ElecZiplineController prev;
        private void Start() {
            self = GetComponent<ProjectileController>();
            body = self.owner.GetComponent<CharacterBody>();
            ziprail = self.GetComponent<ZiprailController>();

            var vehicle = GameObject.Instantiate(Paths.GameObject.ZiprailVehicle, transform);
            ziprail._ziprailVehiclePrefab = Electrician.ZiprailVehicle.GetComponent<ZiprailVehicle>();
            ziprail._vehicleVFX = Paths.GameObject.ZipRailVehicleVFXVFX;
        }
        private void OnEnable() {
            if (!body) {
                return;
            }

            List<ElecZiplineController> list = ActivePylons.GetOrCreateValue(body);
            list.Add(this);
        }

        private void OnDisable() {
            if (!body) {
                return;
            }

            List<ElecZiplineController> list = ActivePylons.GetOrCreateValue(body);
            list.Remove(this);
            
            foreach (ElecZiplineController controller in list) {
                controller.StartCoroutine(controller.OnZiplineCreate());
            }
        }

        public IEnumerator OnZiplineCreate() {
            List<ElecZiplineController> list = ActivePylons.GetOrCreateValue(body);
            int index = list.IndexOf(this);

            ElecZiplineController next = index + 1 >= list.Count ? list[0] : list[index + 1];
            start.transform.position = base.transform.position;

            this.next = null;
            this.prev = null;

            List<BasicBezierSplineControlPoint> points = new();
            GameObject[] destructible = new GameObject[19];

            int max = 20;

            points.Add(start.GetComponent<BasicBezierSplineControlPoint>());

            if (oldPoints != null) {
                for (int i = 0; i < oldPoints.Length; i++) {
                    if (oldPoints[i]) {
                        GameObject.Destroy(oldPoints[i]);
                    }
                }
            }

            if (list.Count == 1) {
                goto end;
            }

            if (index != list.Count - 1) {
                this.next = next;
            }

            if (index != 0) {
                this.prev = list[index - 1];
            }

            for (int i = 1; i < max - 2; i++) {
                GameObject point = new("point");
                point.transform.parent = start.transform.parent;
                point.transform.position = start.position + ((end.position - start.position).normalized * (float)i / (float)max);
                points.Add(point.AddComponent<BasicBezierSplineControlPoint>());
                destructible[i - 1] = point;
            }

            oldPoints = destructible;

            points.Add(end.GetComponent<BasicBezierSplineControlPoint>());

            end.transform.position = next.transform.position;

            base.GetComponent<BasicBezierSpline>().controlPoints = points.ToArray();

            for (int num = ziprail._colliderContainer.transform.childCount - 1; num >= 0; num--)
            {
                Object.Destroy(ziprail._colliderContainer.transform.GetChild(num).gameObject);
            }

            ziprail.path.keyFramesDirty = true;

            yield return new WaitForEndOfFrame();

            ziprail.BakeColliders();
            ziprail.BakeVisuals();

            end:
            yield return null;
        }

        public void FixedUpdate() {
            if (!init)
            {
                initDelay -= Time.fixedDeltaTime;

                if (initDelay <= 0f)
                {
                    init = true;

                    List<ElecZiplineController> list = ActivePylons.GetOrCreateValue(body);
                    list.Add(this);

                    if (list.Count > 3) {
                        ElecZiplineController oldest = list.ElementAt(0);
                        list.Remove(oldest);
                        GameObject.Destroy(oldest.gameObject);
                    }

                    foreach (ElecZiplineController controller in list) {
                        controller.StartCoroutine(controller.OnZiplineCreate());
                    }
                }
            }
        }
    }

    public class ElecZiprailVehicle : MonoBehaviour {
        public ZiprailVehicle self;
        public VehicleSeat seat => self.vehicleSeat;
        private GameObject passengerObject;
        private CharacterBody passengerBody;
        private CharacterMotor passengerCharacterMotor;

        public void Start() {
            self = GetComponent<ZiprailVehicle>();

            seat.onPassengerEnter += OnPassengerEnter;
            seat.onPassengerExit += OnPassengerExit;
        }

        private void OnPassengerEnter(GameObject obj)
        {
            passengerObject = obj;
		    passengerBody = obj.GetComponent<CharacterBody>();
		    passengerCharacterMotor = obj.GetComponent<CharacterMotor>();
        }

        private void OnPassengerExit(GameObject passenger)
        {
            if (passengerCharacterMotor) {
                passengerCharacterMotor.velocity = Vector3.zero;
            }
        }
    }
}