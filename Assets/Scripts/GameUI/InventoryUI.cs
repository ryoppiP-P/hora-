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

    public bool IsOpen => panel != null && panel.activeSelf;

    // 選択モード
    private GrabbableDoor pendingDoor; // 鍵選択待ちのドア

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
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.tabKey.wasPressedThisFrame) {
            if (IsOpen) {
                // 選択モード中の閉じは選択キャンセル
                pendingDoor = null;
            }
            Toggle(!IsOpen);
        }

        // Escでもキャンセル
        if (IsOpen && kb.escapeKey.wasPressedThisFrame) {
            pendingDoor = null;
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
        Toggle(true);
    }

    void OnSlotClicked(int index) {
        var item = inventory.Slots[index]; // まだ取り出さない
        if (item == null) return;

        // 鍵選択モードなら解錠試行
        if (pendingDoor != null) {
            pendingDoor.TryUnlock(item);
            pendingDoor = null;
            Toggle(false);
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
