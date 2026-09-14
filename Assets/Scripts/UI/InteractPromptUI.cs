//==============================================================================
//  File   : InteractPromptUI.cs
//  Brief  : 見ている対象の操作ヒント（[E] 掴んで開く 等）と、
//           投擲物を持っている間の「右クリックで投げる」ヒントを表示する
//==============================================================================
using UnityEngine;
using TMPro;

public class InteractPromptUI : MonoBehaviour {
    [Header("参照")]
    [SerializeField] private Interactor interactor;
    [SerializeField] private ThrowableHolder throwableHolder;
    [SerializeField] private Player player;
    [SerializeField] private PauseManager pauseManager;
    [SerializeField] private InventoryUI inventoryUI;

    [Header("インタラクトヒント（見ている対象に応じて表示）")]
    [SerializeField] private GameObject interactPromptRoot;
    [SerializeField] private TMP_Text interactPromptText;

    [Header("投擲ヒント（投擲物を持っている間、常時表示）")]
    [SerializeField] private GameObject throwHintRoot;
    [SerializeField] private TMP_Text throwHintText;
    [SerializeField] private string throwHintMessage = "[右クリック] 投げる";

    void Update() {
        bool suppressed = IsSuppressed();
        UpdateInteractPrompt(suppressed);
        UpdateThrowHint(suppressed);
    }

    // ポーズ中・インベントリ/ノート閲覧中・死亡後はどちらのヒントも出さない
    bool IsSuppressed() {
        if (player != null && player.IsDead) return true;
        if (pauseManager != null && pauseManager.IsPaused) return true;
        if (inventoryUI != null && inventoryUI.IsOpen) return true;
        if (NoteReaderUI.IsOpen) return true;
        return false;
    }

    void UpdateInteractPrompt(bool suppressed) {
        Interactable target = suppressed ? null : (interactor != null ? interactor.AimingTarget : null);

        if (target == null) {
            SetActive(interactPromptRoot, false);
            return;
        }

        // GrabbableDoorだけ左クリックで掴む方式（他はEキーのホールドで操作する）
        string keyLabel = (target is GrabbableDoor) ? "左クリック" : "E";
        if (interactPromptText != null) interactPromptText.text = "[" + keyLabel + "] " + target.PromptText;
        SetActive(interactPromptRoot, true);
    }

    void UpdateThrowHint(bool suppressed) {
        bool show = !suppressed && throwableHolder != null && throwableHolder.HasItem;
        SetActive(throwHintRoot, show);
        if (show && throwHintText != null) throwHintText.text = throwHintMessage;
    }

    void SetActive(GameObject go, bool active) {
        if (go != null && go.activeSelf != active) go.SetActive(active);
    }
}
