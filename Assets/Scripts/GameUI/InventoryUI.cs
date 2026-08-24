//==============================================================================
//  File   : Inventory.cs
//  Brief  : インベントリUI
// 
//  Author : Ryoto Kikuchi
//  Date   : 2026/7/7
//------------------------------------------------------------------------------
//
//==============================================================================
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryUI : MonoBehaviour {
    [SerializeField] private Inventory inventory;       // インベントリ本体
    [SerializeField] private GameObject panel;          // 表示切替する親
    [SerializeField] private InventorySlot[] slots;     // 9個アサイン
    [SerializeField] private Transform dropPoint;       // プレイヤー前方の空GO
    [SerializeField] private float dropForward = 1.0f;  // ドロップ位置の前方オフセット

    [SerializeField] private ThrowableHolder throwableHolder; // 投げるときのオブジェクト保持用

    [SerializeField] private PauseManager pauseManager;

    public bool IsOpen => panel != null && panel.activeSelf;

    // 選択モード
    private GrabbableDoor pendingDoor;  // 鍵選択待ちの手動ドア
    private AutoDoorLock pendingLock;   // 鍵選択待ちの自動ドア
    private ClearObject pendingClear;   // 鍵選択街の脱出ポッド

    void Start() {
        panel.SetActive(false);
        inventory.OnChanged += Refresh;
        for (int i = 0; i < slots.Length; i++) {
            int idx = i;
            slots[i].Init(idx, OnSlotClicked);
        }
        Refresh();
    }

    void OnDestroy() {
        if (inventory != null) inventory.OnChanged -= Refresh;
    }

    void Update() {
        if (pauseManager != null && pauseManager.IsPaused) return;
        if (NoteReaderUI.IsOpen) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.tabKey.wasPressedThisFrame) {
            if (IsOpen) {
                // 選択モード中の閉じは選択キャンセル
                pendingDoor = null;
                pendingLock = null;
                pendingClear = null;
            }
            Toggle(!IsOpen);
        }

        // Escでもキャンセル
        if (IsOpen && kb.escapeKey.wasPressedThisFrame) {
            pendingDoor = null;
            pendingLock = null;
            pendingClear = null;
            Toggle(false);
        }
    }

    void Toggle(bool open) {
        panel.SetActive(open);
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;
    }

    void Refresh() {
        var list = inventory.Slots;
        for (int i = 0; i < slots.Length; i++) {
            slots[i].SetItem(i < list.Count ? list[i] : null);
        }
    }

    public void OpenForKeySelection(GrabbableDoor door) {
        pendingDoor = door;
        pendingLock = null;
        pendingClear = null;
        Toggle(true);
    }

    public void OpenForKeySelection(AutoDoorLock autoLock) {
        pendingLock = autoLock;
        pendingDoor = null;
        pendingClear = null;
        Toggle(true);
    }

    public void OpenForKeySelection(ClearObject clearObj) {
        pendingClear = clearObj;
        pendingDoor = null;
        pendingLock = null;
        Toggle(true);
    }

    void OnSlotClicked(int index) {
        var item = inventory.Slots[index]; // まだ取り出さない
        if (item == null) return;

        // 鍵選択モード（手動ドア）
        if (pendingDoor != null) {
            pendingDoor.TryUnlock(item);
            pendingDoor = null;
            Toggle(false);
            return;
        }

        // 鍵選択モード（自動ドア）
        if (pendingLock != null) {
            pendingLock.TryUnlock(item);
            pendingLock = null;
            Toggle(false);
            return;
        }

        // 鍵選択モード（脱出ポッド）
        if (pendingClear != null) {
            pendingClear.TryUnlock(item);
            pendingClear = null;
            Toggle(false);
            return;
        }

        // 通常モード：ノートは取り出さず、読み返すだけ
        if (item is NoteItem note) {
            Toggle(false);
            var player = inventory.GetComponent<Player>();
            if (player != null) NoteReaderUI.Show(note, player, null);
            return;
        }

        // 通常モード：写真も取り出さず、読み返すだけ
        if (item is PhotoItem photo) {
            Toggle(false);
            var player = inventory.GetComponent<Player>();
            if (player != null) NoteReaderUI.Show(photo, player, null);
            return;
        }

        // 通常モード：既存の取り出し処理
        var taken = inventory.TakeAt(index);
        if (taken == null || taken.worldPrefab == null) return;

        Vector3 pos = dropPoint != null
            ? dropPoint.position + dropPoint.forward * dropForward
            : transform.position;
        GameObject go = Instantiate(taken.worldPrefab, pos, Quaternion.identity);

        // Throwable が付いていれば ThrowableHolder に持たせる
        var throwable = go.GetComponent<Throwable>();
        if (throwable != null && throwableHolder != null) {
            throwableHolder.Pickup(throwable);
        }

        Toggle(false);
    }
}
