using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class StaminaVisualEffect : MonoBehaviour {
    [SerializeField] private Player player;
    [SerializeField] private Volume volume;

    [Header("Threshold")]
    [Tooltip("この値以下のスタミナ比率から演出開始")]
    [SerializeField] private float startThreshold = 0.5f;

    [Header("Vignette")]
    [SerializeField] private float maxVignetteIntensity = 0.55f;
    [SerializeField] private Color vignetteColor = Color.black;

    [Header("Smoothing")]
    [SerializeField] private float lerpSpeed = 4f;

    private Vignette vignette;
    private float currentWeight = 0f;

    void Start() {
        if (volume != null && volume.profile.TryGet(out Vignette v)) {
            vignette = v;
            vignette.color.overrideState = true;
            vignette.intensity.overrideState = true;
            vignette.color.value = vignetteColor;
            vignette.intensity.value = maxVignetteIntensity;
        }

        if (volume != null) volume.weight = 0f;
    }

    void Update() {
        if (player == null || volume == null) return;

        float ratio = player.StaminaRatio;
        float target = 0f;

        // startThreshold以下で 0→1 に近づく
        if (ratio < startThreshold) {
            target = Mathf.InverseLerp(startThreshold, 0f, ratio);
        }

        // 死亡中は他の演出（溺死）に譲る
        if (player.IsDead) target = 0f;

        currentWeight = Mathf.MoveTowards(currentWeight, target, lerpSpeed * Time.deltaTime);
        volume.weight = currentWeight;
    }
}
