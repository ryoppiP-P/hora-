using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour {
    [Header("Buttons")]
    [SerializeField] private Button startButton;        // ゲーム開始ボタン
    [SerializeField] private Button settingsButton;     // 設定ボタン
    [SerializeField] private Button quitButton;         // 終了ボタン
    [SerializeField] private Button closeSettingsButton;    // 設定パネル閉じるボタン

    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;  // 設定パネル

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.5f;

    private void Start() {
        // タイトルではカーソル表示
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 設定パネルは初期非表示
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        // ボタンイベント登録
        startButton.onClick.AddListener(OnStartClicked);
        settingsButton.onClick.AddListener(OnSettingsClicked);
        quitButton.onClick.AddListener(OnQuitClicked);

        if (closeSettingsButton != null)
            closeSettingsButton.onClick.AddListener(CloseSettings);

        FadeManager.FadeIn(fadeDuration);
    }

    private void Update() {
        // Escで設定画面を閉じる
        if (settingsPanel != null && settingsPanel.activeSelf) {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) {
                CloseSettings();
            }
        }
    }

    private void OnStartClicked() {
        FadeManager.FadeOut(gameSceneName, fadeDuration);
    }

    private void OnSettingsClicked() {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings() {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private void OnQuitClicked() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
