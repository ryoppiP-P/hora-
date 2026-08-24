using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class GrabbableDoor : Interactable {
    [SerializeField] private Camera cam;
    [SerializeField] private Interactor interactor;
    [SerializeField] private float sensitivity = 2f;   // マウス感度
    [SerializeField] private float openSpeed = 200f;   // ドアの回転速度(度/秒)

    [Header("可動域")]
    [SerializeField] private float minAngle = -95f;    // 閉じた状態からの最小角度
    [SerializeField] private float maxAngle = 95f;     // 閉じた状態からの最大角度

    [Header("Lock")]
    [SerializeField] private bool isLocked = false;     // 鍵がかかっているか
    [SerializeField] private ItemBase requiredKey;      // 必要な鍵アイテム
    [SerializeField] private bool consumeKey = true;    // 使ったら消費するか
    [SerializeField] private InventoryUI inventoryUI;   // インベントリUI
    [SerializeField] private Inventory inventory;       // インベントリ
    [SerializeField] private GrabbableDoor[] linkedDoors; // 連動して解錠する他のドア（同じ鍵を使う場合など）

    public override string PromptText => isLocked ? "鍵がかかっている" : "掴んで開く";
    public bool IsLocked => isLocked;

    private Rigidbody rb;
    private BoxCollider box;
    private Collider playerCollider; // 開いている間だけ物理的な押し出しを止める相手
    private bool isGrabbed;
    private float currentAngle;   // 閉じた状態からの現在角度
    private float targetAngle;    // マウス操作で決まる目標角度
    private Quaternion closedLocalRot;

    // 最初から食い込んでいる相手（自分が嵌まっているドア枠など）
    private readonly HashSet<Collider> ignoredColliders = new HashSet<Collider>();

    public static bool IsAnyGrabbing { get; private set; }

    void Awake() {
        rb = GetComponent<Rigidbody>();
        box = GetComponent<BoxCollider>();
        if (cam == null) cam = Camera.main;

        closedLocalRot = transform.localRotation;

        // 扉は常にKinematicで動かす。
        // HingeJointのモーターで回す方式は、枠やプレイヤーと食い込んだ時に
        // PhysXが押し出そうとして無限大の力を出し、位置がNaNになって
        // プレイヤーやカメラまで巻き込んで壊れることがあったため、
        // 物理の力を一切介さずスクリプトが直接姿勢を与える方式にしている。
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        if (interactor != null) playerCollider = interactor.GetComponent<Collider>();
    }

    void Start() {
        IgnoreInitialOverlaps();
    }

    /// <summary>閉じた状態で既に食い込んでいる相手との当たり判定を無効化する</summary>
    void IgnoreInitialOverlaps() {
        if (box == null) return;

        Collider[] hits = Physics.OverlapBox(
            box.bounds.center, box.bounds.extents, transform.rotation, ~0,
            QueryTriggerInteraction.Ignore);

        foreach (var other in hits) {
            if (other == null || other == box) continue;
            if (other.transform.IsChildOf(transform)) continue; // 取っ手など自分の子
            if (other.attachedRigidbody != null) continue;      // 動く物は通常通り当てる

            ignoredColliders.Add(other);
            Physics.IgnoreCollision(box, other, true);
        }
    }

    void Update() {
        var mouse = Mouse.current;
        if (mouse == null) return;

        bool aimingAtMe = (interactor != null
            && interactor.AimingTarget as GrabbableDoor == this);

        if (mouse.leftButton.wasPressedThisFrame && aimingAtMe) {
            if (isLocked) {
                Audio.Post("SE.Player.Door.Hinged.LockedRattle", transform.position);
                if (inventoryUI != null) inventoryUI.OpenForKeySelection(this);
            } else {
                isGrabbed = true;
                IsAnyGrabbing = true;
                targetAngle = currentAngle;
                Audio.Post("SE.Player.Door.Hinged.Open", transform.position);
                // 開いている最中にプレイヤー自身の当たり判定へ食い込んで
                // カメラががくつくのを防ぐため、掴んでいる間だけ衝突を無視する
                if (playerCollider != null) Physics.IgnoreCollision(box, playerCollider, true);
            }
        }
        if (mouse.leftButton.wasReleasedThisFrame) Release();

        if (isGrabbed) {
            // マウスの横移動量を目標角度に加算（可動域内に必ず収める）
            float dx = mouse.delta.ReadValue().x;
            if (!float.IsNaN(dx) && !float.IsInfinity(dx)) {
                targetAngle = Mathf.Clamp(targetAngle + dx * sensitivity, minAngle, maxAngle);
            }
        }
    }

    void FixedUpdate() {
        if (!isGrabbed) return;

        float next = Mathf.MoveTowards(currentAngle, targetAngle, openSpeed * Time.fixedDeltaTime);
        if (Mathf.Approximately(next, currentAngle)) return;

        // 壁などに当たる角度へは進めない
        if (IsBlocked(next)) {
            targetAngle = currentAngle;
            return;
        }

        currentAngle = next;
        rb.MoveRotation(WorldRotationAt(currentAngle));
    }

    /// <summary>指定角度でのワールド回転（扉の板の端＝Transform原点を軸に回る）</summary>
    Quaternion WorldRotationAt(float angle) {
        Quaternion parentRot = transform.parent != null ? transform.parent.rotation : Quaternion.identity;
        return parentRot * closedLocalRot * Quaternion.Euler(0f, angle, 0f);
    }

    /// <summary>その角度に扉を置くと何かにぶつかるか</summary>
    bool IsBlocked(float angle) {
        if (box == null) return false;

        Quaternion worldRot = WorldRotationAt(angle);
        Vector3 scale = transform.lossyScale;
        Vector3 center = transform.position + worldRot * Vector3.Scale(box.center, scale);
        // 枠との僅かな接触で止まらないよう、判定は少し小さめに取る
        Vector3 half = Vector3.Scale(box.size, scale) * 0.45f;

        Collider[] hits = Physics.OverlapBox(center, half, worldRot, ~0, QueryTriggerInteraction.Ignore);
        foreach (var other in hits) {
            if (other == null || other == box) continue;
            if (other.transform.IsChildOf(transform)) continue;
            if (other.attachedRigidbody != null) continue;   // プレイヤーや缶は押しのける
            if (ignoredColliders.Contains(other)) continue;  // 自分が嵌まっている枠
            return true;
        }
        return false;
    }

    public bool TryUnlock(ItemBase item) {
        if (!isLocked) return false;
        if (item == requiredKey) {
            UnlockInternal(true);
            if (consumeKey && inventory != null) inventory.RemoveOne(item);
            Debug.Log($"{name} を解錠");
            Audio.Post("SE.Player.Door.Key.Unlock", transform.position);
            return true;
        }
        Audio.Post("SE.Player.Console.UnlockError");
        Debug.Log("このアイテムじゃ、ない！");
        return false;
    }

    // 連動解錠用（linkedDoorsの無限ループ防止にpropagateフラグ付き、AutoDoorと同じ方式）
    void UnlockInternal(bool propagate) {
        if (!isLocked) return;
        isLocked = false;

        if (propagate && linkedDoors != null) {
            foreach (var d in linkedDoors) {
                if (d != null && d != this) d.UnlockInternal(false);
            }
        }
    }

    void Release() {
        if (isGrabbed) {
            isGrabbed = false;
            IsAnyGrabbing = false;
            if (playerCollider != null) Physics.IgnoreCollision(box, playerCollider, false);
        }
    }

    void OnDisable() {
        if (isGrabbed) {
            isGrabbed = false;
            IsAnyGrabbing = false;
            if (playerCollider != null) Physics.IgnoreCollision(box, playerCollider, false);
        }
    }
}
