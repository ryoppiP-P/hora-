using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour {
    [Header("Mouse Sensitivity")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private TMP_Text sensitivityValueText;    // 現在値表示
    [SerializeField] private float minSensitivity = 0.1f;
    [SerializeField] private float maxSensitivity = 5.0f;

    [Header("Brightness")]
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private TMP_Text brightnessValueText;
    [SerializeField] private Volume globalVolume;              // シーン内のGlobal Volume
    [SerializeField] private float minBrightness = -2.0f;      // Post Exposureの下限
    [SerializeField] private float maxBrightness = 2.0f;

    [Header("Runtime Apply")]
    [Tooltip("同シーン内のCameraLookに感度変更を即反映するために")]
    [SerializeField] private CameraLook cameraLook;

    // PlayerPrefsキー
    public const string KEY_SENSITIVITY = "MouseSensitivity";
    public const string KEY_BRIGHTNESS = "Brightness";

    // デフォルト値
    public const float DEFAULT_SENSITIVITY = 1.0f;
    public const float DEFAULT_BRIGHTNESS = 0.0f;

    private ColorAdjustments colorAdjustments;

    private void Start() {
        // Post-processing の ColorAdjustments を取得
        if (globalVolume != null && globalVolume.profile != null) {
            globalVolume.profile.TryGet(out colorAdjustments);
        }

        // 保存値を読み込みつつ、UIにも反映
        float sens = PlayerPrefs.GetFloat(KEY_SENSITIVITY, DEFAULT_SENSITIVITY);
        float bright = PlayerPrefs.GetFloat(KEY_BRIGHTNESS, DEFAULT_BRIGHTNESS);

        if (sensitivitySlider != null) {
            sensitivitySlider.minValue = minSensitivity;
            sensitivitySlider.maxValue = maxSensitivity;
            sensitivitySlider.value = sens;
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }

        if (brightnessSlider != null) {
            brightnessSlider.minValue = minBrightness;
            brightnessSlider.maxValue = maxBrightness;
            brightnessSlider.value = bright;
            brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
        }

        // 初期値を即反映
        OnSensitivityChanged(sens);
        OnBrightnessChanged(bright);
    }

    private void OnSensitivityChanged(float value) {
        PlayerPrefs.SetFloat(KEY_SENSITIVITY, value);
        if (sensitivityValueText != null)
            sensitivityValueText.text = value.ToString("F2");

        // ゲーム中に開いてる場合はCameraLookに即反映
        if (cameraLook != null)
            cameraLook.ApplySensitivity(value);
    }

    private void OnBrightnessChanged(float value) {
        PlayerPrefs.SetFloat(KEY_BRIGHTNESS, value);
        if (brightnessValueText != null)
            brightnessValueText.text = value.ToString("F2");

        if (colorAdjustments != null)
            colorAdjustments.postExposure.value = value;
    }

    private void OnDisable() {
        PlayerPrefs.Save();
    }
}
