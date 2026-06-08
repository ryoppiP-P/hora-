using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour {
    public enum MoveState { Walk, Run, Crouch }

    [Header("Move Speeds")]
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float runSpeed = 5.0f;
    [SerializeField] private float crouchSpeed = 0.83f;

    [Header("Physics")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 1.2f;

    [Header("Crouch")]
    [SerializeField] private float standHeight = 1.8f;
    [SerializeField] private float crouchHeight = 0.9f;
    [SerializeField] private float standCameraY = 1.6f;
    [SerializeField] private float crouchCameraY = 0.8f;
    [SerializeField] private float crouchLerpSpeed = 10f;
    [SerializeField] private Transform cameraTransform;

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 6f;       // 走行可能秒数
    [SerializeField] private float recoverDuration = 12f; // 0→満タンまでの秒数
    [SerializeField] private float currentStamina;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.2f;
    [SerializeField] private LayerMask groundMask;

    [Header("Interact")]
    [SerializeField] private Interactor interactor;
    [SerializeField] private ThrowableHolder holder;

    // 状態
    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private MoveState state = MoveState.Walk;
    private bool staminaExhausted; // 一度切れたら回復するまで走れない

    // 公開プロパティ（CameraLook が参照）
    public MoveState CurrentState => state;
    public float StaminaRatio => currentStamina / maxStamina;
    public bool IsMoving { get; private set; }

    void Start() {
        controller = GetComponent<CharacterController>();
        currentStamina = maxStamina;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update() {
        var kb = Keyboard.current;
        if (kb == null) return;

        // 接地判定
        if (groundCheck != null)
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        else
            isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        // 入力
        Vector2 input = Vector2.zero;
        if (kb.wKey.isPressed) input.y += 1;
        if (kb.sKey.isPressed) input.y -= 1;
        if (kb.dKey.isPressed) input.x += 1;
        if (kb.aKey.isPressed) input.x -= 1;

        bool wantRun = kb.leftShiftKey.isPressed;
        bool wantCrouch = kb.leftCtrlKey.isPressed;
        IsMoving = input.sqrMagnitude > 0.01f;

        // ステート決定（しゃがみ優先）
        if (wantCrouch) {
            state = MoveState.Crouch;
        }
        else if (wantRun && IsMoving && !staminaExhausted) {
            state = MoveState.Run;
        }
        else {
            state = MoveState.Walk;
        }

        bool locked = (interactor != null && interactor.IsInteracting)
           || (holder != null && holder.IsAiming);

        if (locked) {
            input = Vector2.zero;
            wantRun = false;
            // 必要なら state も Walk に強制
        }

        // スタミナ更新
        UpdateStamina();

        // 速度
        float speed = state switch {
            MoveState.Run => runSpeed,
            MoveState.Crouch => crouchSpeed,
            _ => walkSpeed,
        };

        // しゃがみによるコライダー＆カメラ高さ更新
        UpdateCrouchTransition();

        // 水平移動
        Vector3 move = transform.right * input.x + transform.forward * input.y;
        if (move.sqrMagnitude > 1f) move.Normalize(); // 斜め入力で速くなるのを防ぐ

        // ジャンプ（しゃがみ中は不可）
        if (kb.spaceKey.wasPressedThisFrame && isGrounded && state != MoveState.Crouch)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        // 重力
        velocity.y += gravity * Time.deltaTime;

        // 1回のMoveでまとめて適用（velocityプロパティを正しく取るため）
        Vector3 finalMove = move * speed + new Vector3(0f, velocity.y, 0f);
        controller.Move(finalMove * Time.deltaTime);

        // Esc
        if (kb.escapeKey.wasPressedThisFrame)
            Cursor.lockState = CursorLockMode.None;
    }

    void UpdateStamina() {
        if (state == MoveState.Run) {
            currentStamina -= Time.deltaTime;
            if (currentStamina <= 0f) {
                currentStamina = 0f;
                staminaExhausted = true; // 強制歩行へ
            }
        }
        else {
            // 12秒で0→満タン
            float recoverPerSec = maxStamina / recoverDuration;
            currentStamina += recoverPerSec * Time.deltaTime;
            if (currentStamina >= maxStamina) {
                currentStamina = maxStamina;
                staminaExhausted = false;
            }
        }
    }

    void UpdateCrouchTransition() {
        float targetHeight = (state == MoveState.Crouch) ? crouchHeight : standHeight;
        float targetCamY = (state == MoveState.Crouch) ? crouchCameraY : standCameraY;

        // CharacterController の高さとセンターを補間
        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchLerpSpeed);
        Vector3 c = controller.center;
        c.y = controller.height * 0.5f;
        controller.center = c;

        // カメラ高さ
        if (cameraTransform != null) {
            Vector3 cp = cameraTransform.localPosition;
            cp.y = Mathf.Lerp(cp.y, targetCamY, Time.deltaTime * crouchLerpSpeed);
            cameraTransform.localPosition = cp;
        }
    }
}
