using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(HingeJoint), typeof(Rigidbody))]
public class GrabbableDoor : MonoBehaviour {
    [SerializeField] private Camera cam;
    [SerializeField] private float grabDistance = 1.5f;
    [SerializeField] private float sensitivity = 2f;   // マウス感度
    [SerializeField] private float openSpeed = 200f;   // ドアの回転速度

    [Header("Lock")]
    [SerializeField] private bool isLocked = false;     // 鍵がかかっているか
    [SerializeField] private ItemBase requiredKey;      // 必要な鍵アイテム
    [SerializeField] private bool consumeKey = true;    // 使ったら消費するか
    [SerializeField] private InventoryUI inventoryUI;   // インベントリUI
    [SerializeField] private Inventory inventory;       // インベントリ

    public bool IsLocked => isLocked;

    private HingeJoint hinge;
    private Rigidbody rb;
    private bool isGrabbed;
    private float targetAngle;

    public static bool IsAnyGrabbing { get; private set; }

    void Awake() {
        hinge = GetComponent<HingeJoint>();
        rb = GetComponent<Rigidbody>();
        if (cam == null) cam = Camera.main;

        var motor = hinge.motor;
        motor.force = 100f;
        motor.targetVelocity = 0f;
        motor.freeSpin = false;
        hinge.motor = motor;
        hinge.useMotor = true;
    }

    void Update() {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame) {
            if (isLocked && IsAimingAtMe()) {
                inventoryUI.OpenForKeySelection(this);
            } else {
                TryGrab();
            }
        }
        if (mouse.leftButton.wasReleasedThisFrame) Release();

        if (isGrabbed) {
            // マウスの横移動量を目標角度に加算
            float dx = mouse.delta.ReadValue().x;
            targetAngle += dx * sensitivity;

            // HingeJoint の Limits 範囲内にクランプ
            if (hinge.useLimits) {
                targetAngle = Mathf.Clamp(targetAngle, hinge.limits.min, hinge.limits.max);
            }
        }
    }

    void FixedUpdate() {
        if (!isGrabbed) {
            // 掴んでいない時は動かさない
            var motor = hinge.motor;
            motor.targetVelocity = 0f;
            hinge.motor = motor;
            return;
        }

        // 現在角度と目標角度の差から回転速度を決める
        float diff = targetAngle - hinge.angle;
        var m = hinge.motor;
        m.targetVelocity = Mathf.Clamp(diff * 5f, -openSpeed, openSpeed);
        hinge.motor = m;
    }

    void TryGrab() {
        if (cam == null) return;
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out var hit, grabDistance)) {
            if (hit.collider.gameObject == gameObject
                || hit.collider.transform.IsChildOf(transform)) {
                isGrabbed = true;
                IsAnyGrabbing = true;
                targetAngle = hinge.angle; // 現在角度から開始
            }
        }
    }

    bool IsAimingAtMe() {
        if (cam == null) return false;
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out var hit, grabDistance)) {
            return hit.collider.gameObject == gameObject
                || hit.collider.transform.IsChildOf(transform);
        }
        return false;
    }

    // InventoryUI から呼ばれる：選択したアイテムで解錠試行
    public bool TryUnlock(ItemBase item) {
        if (!isLocked) return false;
        if (item == requiredKey) {
            isLocked = false;
            if (consumeKey) {
                // インベントリから消費
                RemoveFromInventory(item);
            }
            Debug.Log($"{name} を解錠");
            return true;
        }
        Debug.Log("このアイテムじゃ、ない！");
        return false;
    }

    void RemoveFromInventory(ItemBase item) {
        inventory.RemoveOne(item);
    }

    void Release() {
        if (isGrabbed) {
            isGrabbed = false;
            IsAnyGrabbing = false;
        }
    }

    void OnDisable() {
        if (isGrabbed) {
            isGrabbed = false;
            IsAnyGrabbing = false;
        }
    }
}
