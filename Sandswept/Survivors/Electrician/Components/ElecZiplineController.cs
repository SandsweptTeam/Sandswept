using System;
using RoR2BepInExPack.Utilities;

namespace Sandswept.Survivors.Electrician {
    public class ElecZiplineController : MonoBehaviour {
        private ZiprailController ziprail;
        private ProjectileController self;
        internal static FixedConditionalWeakTable<CharacterBody, List<ElecZiplineController>> ActivePylons = new();
        private CharacterBody body;
        private bool init = false;
        private float initDelay = 2f;
        public Transform start;
        public Transform end;
        private GameObject[] oldPoints;
        private void Start() {
            self = GetComponent<ProjectileController>();
            body = self.owner.GetComponent<CharacterBody>();
            ziprail = self.GetComponent<ZiprailController>();

            var vehicle = GameObject.Instantiate(Paths.GameObject.ZiprailVehicle, transform);
            ziprail._ziprailVehiclePrefab = vehicle.GetComponent<ZiprailVehicle>();
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
                controller.OnZiplineCreate();
            }
        }

        public void OnZiplineCreate() {
            List<ElecZiplineController> list = ActivePylons.GetOrCreateValue(body);
            int index = list.IndexOf(this);

            ElecZiplineController next = index + 1 >= list.Count ? list[0] : list[index + 1];
            start.transform.position = base.transform.position;

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
                return;
            }

            if (list.Count == 2 && index == 1) {
                return;
            }

            for (int i = 1; i < max - 1; i++) {
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

            ziprail.BakeColliders();
            ziprail.BakeVisuals();
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

                    foreach (ElecZiplineController controller in list) {
                        controller.OnZiplineCreate();
                    }
                }
            }
        }
    }
}