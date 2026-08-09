// ゲームシーンに明るさを適用
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BrightnessApplier : MonoBehaviour {
    [SerializeField] private Volume globalVolume;

    private void Start() {
        if (globalVolume == null) globalVolume = GetComponent<Volume>();
        if (globalVolume == null || globalVolume.profile == null) return;

        if (globalVolume.profile.TryGet(out ColorAdjustments ca)) {
            ca.postExposure.value = PlayerPrefs.GetFloat(
                SettingsManager.KEY_BRIGHTNESS,
                SettingsManager.DEFAULT_BRIGHTNESS
            );
        }
    }
}