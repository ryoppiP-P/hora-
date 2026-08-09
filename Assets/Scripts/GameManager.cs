using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour {
    [Header("References")]
    [SerializeField] private Player player;

    [Header("Death UI")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private Button respawnButton;
    [SerializeField] private Button titleButton;

    [Header("Clear UI")]
    [SerializeField] private GameObject clearPanel;
    [SerializeField] private Button clearToTitleButton;

    [Header("Scene")]
    [SerializeField] private string titleSceneName = "Title";

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.5f;

    private bool isGameOver = false;

    private void Start() {
        if (deathPanel != null) deathPanel.SetActive(false);
        if (clearPanel != null) clearPanel.SetActive(false);

        if (player != null)
            player.OnDeath += HandlePlayerDeath;

        if (respawnButton != null)
            respawnButton.onClick.AddListener(Respawn);
        if (titleButton != null)
            titleButton.onClick.AddListener(ReturnToTitle);
        if (clearToTitleButton != null)
            clearToTitleButton.onClick.AddListener(ReturnToTitle);

        FadeManager.FadeIn(fadeDuration);
    }

    private void OnDestroy() {
        if (player != null)
            player.OnDeath -= HandlePlayerDeath;
    }

    private void HandlePlayerDeath() {
        if (isGameOver) return;
        isGameOver = true;

        Debug.Log($"[GameManager] Player is Die...");

        if (deathPanel != null)
            deathPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void TriggerClear() {
        if (isGameOver) return;
        isGameOver = true;

        Debug.Log("[GameManager] Game Clear");

        if (clearPanel != null)
            clearPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Respawn() {
        // 同シーンをフェードで再ロード
        string currentScene = SceneManager.GetActiveScene().name;
        FadeManager.FadeOut(currentScene, fadeDuration);
    }

    private void ReturnToTitle() {
        FadeManager.FadeOut(titleSceneName, fadeDuration);
    }
}
