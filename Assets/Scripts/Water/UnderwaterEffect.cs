using UnityEngine;
using UnityEngine.Rendering;

public class UnderwaterEffect : MonoBehaviour {
    [SerializeField] private PlayerWaterEffect waterEffect;
    [SerializeField] private Volume underwaterVolume;

    [Header("Threshold & Fade")]
    [SerializeField] private float activateThreshold = 0.85f;  // この浸水率から効果開始
    [SerializeField] private float fadeSpeed = 3f;             // フェード速度

    private float currentWeight = 0f;

    void Update() {
        if (waterEffect == null || underwaterVolume == null) return;

        // 目標weight（浸水率がthresholdを超えたら徐々に1へ）
        float targetWeight = Mathf.InverseLerp(activateThreshold, 1f, waterEffect.SubmergeRatio);

        // 滑らかにフェード（水から出た時も徐々に弱まる）
        currentWeight = Mathf.MoveTowards(currentWeight, targetWeight, fadeSpeed * Time.deltaTime);

        underwaterVolume.weight = currentWeight;
    }
}
