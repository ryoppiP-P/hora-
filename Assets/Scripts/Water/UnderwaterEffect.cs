using UnityEngine;
using UnityEngine.Rendering;

public class UnderwaterEffect : MonoBehaviour {
    [SerializeField] private PlayerWaterEffect waterEffect;
    [SerializeField] private Player player;
    [SerializeField] private Volume underwaterVolume;

    [Header("Threshold & Fade")]
    [SerializeField] private float activateThreshold = 0.85f;  // この浸水率から効果開始
    [SerializeField] private float fadeSpeed = 3f;             // フェード速度

    private float currentWeight = 0f;

    void Update() {
        if (waterEffect == null || underwaterVolume == null) return;

        // 浸水率ベース（0.85～1.0 で 0～1）
        float waterWeight = Mathf.InverseLerp(activateThreshold, 1f, waterEffect.SubmergeRatio);

        // 溺死進行度をブースト（水没しきってからさらに悪化）
        float drownWeight = player != null ? player.DrownProgress : 0f;

        // 大きい方を採用
        float targetWeight = Mathf.Max(waterWeight, drownWeight);

        // 滑らかにフェード（水から出た時も徐々に弱まる）
        currentWeight = Mathf.MoveTowards(currentWeight, targetWeight, fadeSpeed * Time.deltaTime);

        underwaterVolume.weight = currentWeight;
    }
}
