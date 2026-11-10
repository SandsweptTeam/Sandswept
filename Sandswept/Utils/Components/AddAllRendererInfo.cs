using System;
using System.Linq;
using static RoR2.CharacterModel;

namespace Sandswept.Utils.Components {
    public class AddAllRendererInfo : MonoBehaviour {
        public RendererInfo referenceInfo;

        public Transform[] roots;

        public void Start() {
            CharacterModel cm = GetComponent<CharacterModel>();
            List<RendererInfo> infos = cm.baseRendererInfos.ToList();

            foreach (Transform root in roots) {
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

                foreach (Renderer renderer in renderers) {
                    RendererInfo info = default;
                    info.renderer = renderer;
                    info.defaultMaterial = referenceInfo.defaultMaterial;
                    info.defaultShadowCastingMode = referenceInfo.defaultShadowCastingMode;
                    info.hideOnDeath = referenceInfo.hideOnDeath;
                    info.ignoresMaterialOverrides = referenceInfo.ignoresMaterialOverrides;
                    info.ignoreOverlays = referenceInfo.ignoreOverlays;
                    
                    infos.Add(info);
                }
            }

            cm.baseRendererInfos = infos.ToArray();
        }
    }
}