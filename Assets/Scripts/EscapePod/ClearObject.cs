using UnityEngine;

public class ClearObject : Interactable {
    [Header("References")]
    [SerializeField] private ItemBase requiredKey;      // 必要な鍵（不要ならnull）
    [SerializeField] private Inventory playerInventory;
    [SerializeField] private InventoryUI inventoryUI;

    [Header("State")]
    [SerializeField] private bool consumeKey = true;    // 鍵を消費するか

    [Header("Escape Pod Parts")]
    [SerializeField] private Transform hatch;                                  // 開くハッチ（任意）
    [SerializeField] private Vector3 hatchOpenOffset = new Vector3(0f, 1.3f, 0f);
    [SerializeField] private float hatchOpenSpeed = 0.5f;

    [Header("Effects")]
    [SerializeField] private ParticleSystem steamEffect;
    [SerializeField] private Light indicatorLight;
    [SerializeField] private Color unlockedColor = Color.green;

    [Header("ロックランプ (コンソール上部のへこみの発光板)")]
    [SerializeField] private Renderer lockLamp;   // 発光板のRenderer
    [SerializeField] private Color lampLockedColor = new Color(1f, 0.08f, 0.06f);   // 起動前の色
    [SerializeField] private Color lampUnlockedColor = new Color(0.1f, 1f, 0.25f);  // 起動後の色
    [SerializeField] private float lampBrightness = 1f;

    [Header("Clear Delay")]
    [Tooltip("起動からクリア画面表示までの待機時間(秒)")]
    [SerializeField] private float clearDelay = 2.5f;

    [Header("Scene")]
    [SerializeField] private GameManager gameManager;

    [Header("Ending Cutscene")]
    public EndingCutscene endingCutscene;

    public override string PromptText => "脱出ポッドを起動";

    private bool isActivated = false;
    public event System.Action OnActivated;

    private Vector3 hatchStartPos;
    private Vector3 hatchTargetPos;

    private void Start() {
        if (hatch != null) {
            hatchStartPos = hatch.localPosition;
            hatchTargetPos = hatchStartPos;
        }
        ApplyLampColor();
    }

#if UNITY_EDITOR
    // Inspectorで色をいじった時、エディタ上でも反映されるようにしておく
    private void OnValidate() {
        ApplyLampColor();
    }
#endif

    /// <summary>起動前は赤、起動後は緑にランプを光らせる</summary>
    private void ApplyLampColor() {
        if (lockLamp == null) return;

        Color baseColor = isActivated ? lampUnlockedColor : lampLockedColor;
        Color c = new Color(baseColor.r * lampBrightness,
                            baseColor.g * lampBrightness,
                            baseColor.b * lampBrightness, 1f);

        // マテリアルは共有アセットなので、個体ごとの色はPropertyBlockで与える
        var block = new MaterialPropertyBlock();
        lockLamp.GetPropertyBlock(block);
        block.SetColor("_BaseColor", c);
        block.SetColor("_Color", c);
        block.SetColor("_EmissionColor", c);
        lockLamp.SetPropertyBlock(block);
    }

    /// <summary>エンディング演出用：開いたドアを閉じ始める</summary>
    public void CloseHatch() {
        if (hatch == null) return;
        hatchTargetPos = hatchStartPos;
    }

    /// <summary>ドアが閉じ切ったか</summary>
    public bool IsHatchClosed {
        get {
            if (hatch == null) return true;
            return (hatch.localPosition - hatchStartPos).sqrMagnitude < 0.0001f;
        }
    }

    private void Update() {
        if (isActivated && hatch != null) {
            hatch.localPosition = Vector3.MoveTowards(
                hatch.localPosition, hatchTargetPos, hatchOpenSpeed * Time.deltaTime);
        }
    }

    public override void OnInteractComplete(Player player) {
        if (isActivated) return;

        // 鍵不要ならそのまま起動
        if (requiredKey == null) {
            Activate();
            return;
        }

        // 鍵選択モードでインベントリ展開
        if (inventoryUI != null)
            inventoryUI.OpenForKeySelection(this);
    }

    /// <summary>InventoryUIから呼ばれる：鍵アイテムを受け取って判定</summary>
    public void TryUnlock(ItemBase item) {
        if (isActivated) return;

        if (item == requiredKey) {
            if (consumeKey && playerInventory != null)
                playerInventory.RemoveOne(item);
            Activate();
        } else {
            Audio.Post("SE.Player.Console.UnlockError");
            Debug.Log("[ClearObject] 鍵が違う");
        }
    }

    private void Activate() {
        isActivated = true;
        OnActivated?.Invoke();
        Audio.Post("SE.Player.EscapePod.Unlock", transform.position);
        Debug.Log("[ClearObject] 脱出ポッド起動");

        if (steamEffect != null) steamEffect.Play();
        if (indicatorLight != null) indicatorLight.color = unlockedColor;
        if (hatch != null) hatchTargetPos = hatchStartPos + hatchOpenOffset;
        ApplyLampColor();

        Invoke(nameof(FireClear), clearDelay);
    }

    private void FireClear() {
        if (endingCutscene != null) {
            StartCoroutine(endingCutscene.Play());
        } else if (gameManager != null) {
            gameManager.TriggerClear();
        }
    }
}
