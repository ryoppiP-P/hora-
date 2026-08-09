using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour {
    [Header("References")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button returnToTitleButton;
    [SerializeField] private Player player;
    [SerializeField] private InventoryUI inventoryUI;

    [Header("Scene")]
    [SerializeField] private string titleSceneName = "Title";
    [SerializeField] private float fadeDuration = 0.5f;

    public bool IsPaused { get; private set; }

    private void Start() {
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        if (returnToTitleButton != null)
            returnToTitleButton.onClick.AddListener(OnReturnToTitle);
    }

    private void Update() {
        var kb = Keyboard.current;
        if (kb == null) return;

        // インベントリ開いてる時はポーズ操作を無効化（Inventory優先）
        if (inventoryUI != null && inventoryUI.IsOpen) return;

        if (kb.escapeKey.wasPressedThisFrame) {
            if (IsPaused) Close();
            else Open();
        }
    }

    public void Open() {
        if (settingsPanel == null) return;
        settingsPanel.SetActive(true);
        IsPaused = true;

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (player != null) player.SetInputLocked(true);
    }

    public void Close() {
        if (settingsPanel == null) return;
        settingsPanel.SetActive(false);
        IsPaused = false;

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (player != null) player.SetInputLocked(false);
    }

    private void OnReturnToTitle() {
        Time.timeScale = 1f;
        FadeManager.FadeOut(titleSceneName, fadeDuration);
    }
}
