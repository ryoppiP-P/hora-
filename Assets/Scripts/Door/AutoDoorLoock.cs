using UnityEngine;

public class AutoDoorLock : Interactable {
    [Header("References")]
    [SerializeField] private AutoDoor door;
    [SerializeField] private ItemBase requiredKey;    // 必要な鍵（不要ならnull）
    [SerializeField] private Inventory playerInventory;
    [SerializeField] private InventoryUI inventoryUI;

    [Header("State")]
    [SerializeField] private bool isLocked = true;
    [SerializeField] private bool consumeKey = true;  // 鍵を消費するか

    public override string PromptText => isLocked ? "解錠する" : "開ける";

    public override void OnInteractComplete(Player player) {
        if (!isLocked) {
            // すでに解錠済み → ドア開閉トグル
            door.Toggle();
            return;
        }

        // 鍵不要ならそのまま解錠
        if (requiredKey == null) {
            Unlock();
            return;
        }

        // 鍵選択モードでインベントリ展開
        if (inventoryUI != null)
            inventoryUI.OpenForKeySelection(this);
    }

    /// <summary>InventoryUIから呼ばれる：鍵アイテムを受け取って判定</summary>
    public void TryUnlock(ItemBase item) {
        if (!isLocked) return;

        if (item == requiredKey) {
            if (consumeKey && playerInventory != null)
                playerInventory.RemoveOne(item);
            Unlock();
        } else {
            Audio.Post("SE.Player.Console.UnlockError");
            Debug.Log("[AutoDoorLock] 鍵が違う");
        }
    }

    private void Unlock() {
        isLocked = false;
        Debug.Log("[AutoDoorLock] 解錠");
        Audio.Post("SE.Player.Door.Large.Unlock", transform.position);
        door.Open();
    }
}
