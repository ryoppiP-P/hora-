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
        // 同シーンを再ロード（最も簡単な方法）
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ReturnToTitle() {
        SceneManager.LoadScene(titleSceneName);
    }
}
