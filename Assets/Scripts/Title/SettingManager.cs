using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour {
    [Header("Mouse Sensitivity")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private TMP_Text sensitivityValueText;    // 現在値表示
    [SerializeField] private float minSensitivity = 0.1f;
    [SerializeField] private float maxSensitivity = 5.0f;

    [Header("Volume")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Text volumeValueText;
    [SerializeField] private float minVolume = 0.0f;
    [SerializeField] private float maxVolume = 1.0f;

    [Header("Runtime Apply")]
    [Tooltip("同シーン内のCameraLookに感度変更を即反映するために")]
    [SerializeField] private CameraLook cameraLook;

    // PlayerPrefsキー
    public const string KEY_SENSITIVITY = "MouseSensitivity";
    public const string KEY_VOLUME = "Volume";

    // デフォルト値
    public const float DEFAULT_SENSITIVITY = 1.0f;
    public const float DEFAULT_VOLUME = 1.0f;

    private void Start() {
        // 保存値を読み込みつつ、UIにも反映
        float sens = PlayerPrefs.GetFloat(KEY_SENSITIVITY, DEFAULT_SENSITIVITY);
        float vol = PlayerPrefs.GetFloat(KEY_VOLUME, DEFAULT_VOLUME);

        if (sensitivitySlider != null) {
            sensitivitySlider.minValue = minSensitivity;
            sensitivitySlider.maxValue = maxSensitivity;
            sensitivitySlider.value = sens;
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }

        if (volumeSlider != null) {
            volumeSlider.minValue = minVolume;
            volumeSlider.maxValue = maxVolume;
            volumeSlider.value = vol;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        // 初期値を即反映
        OnSensitivityChanged(sens);
        OnVolumeChanged(vol);
    }

    private void OnSensitivityChanged(float value) {
        PlayerPrefs.SetFloat(KEY_SENSITIVITY, value);
        if (sensitivityValueText != null)
            sensitivityValueText.text = value.ToString("F2");

        // ゲーム中に開いてる場合はCameraLookに即反映
        if (cameraLook != null)
            cameraLook.ApplySensitivity(value);
    }

    private void OnVolumeChanged(float value) {
        PlayerPrefs.SetFloat(KEY_VOLUME, value);
        if (volumeValueText != null)
            volumeValueText.text = value.ToString("F2");

        AudioManager audioManager = AudioManager.GetOrCreate();
        audioManager.SetMixerVolume("BGMVolume", value);
        audioManager.SetMixerVolume("SEVolume", value);
    }

    private void OnDisable() {
        PlayerPrefs.Save();
    }
}