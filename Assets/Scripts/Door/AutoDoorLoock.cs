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

    [Header("ロックランプ (コンソール上部のへこみの発光板)")]
    [SerializeField] private Renderer lockLamp;   // 発光板のRenderer
    [SerializeField] private Color lockedColor = new Color(1f, 0.08f, 0.06f);   // 施錠中の色
    [SerializeField] private Color unlockedColor = new Color(0.1f, 1f, 0.25f);  // 解錠後の色
    [SerializeField] private float lampBrightness = 1f; // 明るさ（Bloomを入れるなら1より上げると光る）

    public override string PromptText => isLocked ? "解錠する" : "開ける";

    void Start() {
        // Bossの通行判定用に、ドア側へ現在の鍵状態を同期しておく
        if (door != null) door.SetLocked(isLocked);
        ApplyLampColor();
    }

#if UNITY_EDITOR
    // Inspectorでフラグや色をいじった時、エディタ上でも色が変わるようにしておく
    void OnValidate() {
        ApplyLampColor();
    }
#endif

    /// <summary>施錠中は赤、解錠後は緑にランプを光らせる</summary>
    private void ApplyLampColor() {
        if (lockLamp == null) return;

        Color baseColor = isLocked ? lockedColor : unlockedColor;
        Color c = new Color(baseColor.r * lampBrightness,
                            baseColor.g * lampBrightness,
                            baseColor.b * lampBrightness, 1f);

        // マテリアル自体は共有アセットなので、個体ごとの色はPropertyBlockで与える
        var block = new MaterialPropertyBlock();
        lockLamp.GetPropertyBlock(block);
        block.SetColor("_BaseColor", c);
        block.SetColor("_Color", c);
        block.SetColor("_EmissionColor", c);
        lockLamp.SetPropertyBlock(block);
    }

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
        if (door != null) door.SetLocked(false);
        ApplyLampColor();
        Debug.Log("[AutoDoorLock] 解錠");
        Audio.Post("SE.Player.Door.Large.Unlock", transform.position);
        door.Open();
    }
}
