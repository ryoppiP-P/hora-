using UnityEngine;
using UnityEngine.InputSystem;

public class CameraLook : MonoBehaviour {
    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private Transform playerBody;

    [Header("Head Bob")]
    [SerializeField] private bool enableHeadBob = true;
    [SerializeField] private Player player;
    [SerializeField] private Rigidbody playerRigidbody;
    [SerializeField] private float bobSmoothing = 10f;

    // 歩行（微量）
    [SerializeField] private float walkBobFreq = 8f;
    [SerializeField] private float walkBobAmpY = 0.03f;
    [SerializeField] private float walkBobAmpX = 0.02f;

    // 走行（やや大）
    [SerializeField] private float runBobFreq = 12f;
    [SerializeField] private float runBobAmpY = 0.07f;
    [SerializeField] private float runBobAmpX = 0.04f;

    [SerializeField] private InventoryUI inventoryUI;   // インベントリUI（カメラの動き止めるのに使う）

    private float xRotation = 0f;
    private float bobTimer = 0f;
    // しゃがみでカメラ高さが変わるので、Y基準は毎フレーム現在値を使う
    private Vector3 currentBobOffset;
    private Vector3 prevAppliedBob = Vector3.zero;


    void LateUpdate() {
        if (player == null || player.IsDead == true) return;
        if (inventoryUI != null && inventoryUI.IsOpen) return;
        if (GrabbableDoor.IsAnyGrabbing) return;
        HandleLook();
        HandleHeadBob();
    }

    void HandleLook() {
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 delta = mouse.delta.ReadValue() * mouseSensitivity;

        xRotation -= delta.y;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        if (playerBody != null)
            playerBody.Rotate(Vector3.up * delta.x);
    }

    void HandleHeadBob() {
        if (!enableHeadBob || playerRigidbody == null || player == null) return;

        // Rigidbodyの水平速度を取得
        Vector3 horizontalVel = playerRigidbody.linearVelocity;
        horizontalVel.y = 0f;
        float speed = horizontalVel.magnitude;

        Vector3 targetOffset = Vector3.zero;

        // 接地判定はPlayer側の値を使いたいが公開されていないので、速度と歩行状態で近似
        bool isMoving = speed > 0.1f;

        if (isMoving && player.CurrentState != Player.MoveState.Crouch) {
            float freq, ampY, ampX;
            if (player.CurrentState == Player.MoveState.Run) {
                freq = runBobFreq; ampY = runBobAmpY; ampX = runBobAmpX;
            } else {
                freq = walkBobFreq; ampY = walkBobAmpY; ampX = walkBobAmpX;
            }

            bobTimer += Time.deltaTime * freq;
            float y = Mathf.Sin(bobTimer) * ampY;
            float x = Mathf.Cos(bobTimer * 0.5f) * ampX;
            targetOffset = new Vector3(x, y, 0f);
        } else {
            bobTimer = 0f;
        }

        currentBobOffset = Vector3.Lerp(currentBobOffset, targetOffset, Time.deltaTime * bobSmoothing);

        // 1フレーム前の bob を打ち消してから新しい bob を足す
        Vector3 lp = transform.localPosition;
        lp -= prevAppliedBob;
        lp += currentBobOffset;
        transform.localPosition = lp;
        prevAppliedBob = currentBobOffset;
    }
}
