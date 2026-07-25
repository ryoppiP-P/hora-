using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(HingeJoint), typeof(Rigidbody))]
public class GrabbableDoor : Interactable {
    [SerializeField] private Camera cam;
    [SerializeField] private Interactor interactor;
    [SerializeField] private float sensitivity = 2f;   // マウス感度
    [SerializeField] private float openSpeed = 200f;   // ドアの回転速度

    [Header("Lock")]
    [SerializeField] private bool isLocked = false;     // 鍵がかかっているか
    [SerializeField] private ItemBase requiredKey;      // 必要な鍵アイテム
    [SerializeField] private bool consumeKey = true;    // 使ったら消費するか
    [SerializeField] private InventoryUI inventoryUI;   // インベントリUI
    [SerializeField] private Inventory inventory;       // インベントリ

    public override string PromptText => isLocked ? "鍵がかかっている" : "掴んで開く";
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

        bool aimingAtMe = (interactor != null
            && interactor.AimingTarget as GrabbableDoor == this);

        if (mouse.leftButton.wasPressedThisFrame && aimingAtMe) {
            if (isLocked) {
                inventoryUI.OpenForKeySelection(this);
            } else {
                isGrabbed = true;
                IsAnyGrabbing = true;
                targetAngle = hinge.angle;
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

    public bool TryUnlock(ItemBase item) {
        if (!isLocked) return false;
        if (item == requiredKey) {
            isLocked = false;
            if (consumeKey) inventory.RemoveOne(item);
            Debug.Log($"{name} を解錠");
            return true;
        }
        Debug.Log("このアイテムじゃ、ない！");
        return false;
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
