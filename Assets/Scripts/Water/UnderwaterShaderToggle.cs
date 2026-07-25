//using UnityEngine;
//using UnityEngine.Rendering.Universal;

//public class UnderwaterShaderToggle : MonoBehaviour {
//    [SerializeField] private PlayerWaterEffect waterEffect;
//    [SerializeField] private ScriptableRendererFeature underwaterFeature;
//    [SerializeField] private float activateThreshold = 0.5f;

//    void Update() {
//        if (waterEffect == null || underwaterFeature == null) return;
//        underwaterFeature.SetActive(waterEffect.SubmergeRatio >= activateThreshold);
//    }
//}
