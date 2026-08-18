using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class Player : MonoBehaviour {
    public enum MoveState { Walk, Run, Crouch }

    [Header("Move Speeds")]
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float runSpeed = 5.0f;
    [SerializeField] private float crouchSpeed = 0.83f;

    [Header("Physics")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float acceleration = 15f;   // 目標速度への追従の速さ

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

    [Header("Footstep SE")]
    [SerializeField] private float walkStepInterval = 0.5f;        // 歩行時の足音間隔(秒)
    [SerializeField] private float runStepInterval = 0.35f;        // 走行時の足音間隔(秒)
    [SerializeField] private float underwaterStepInterval = 0.6f;  // 水中歩行時の足音間隔(秒)

    [Header("Interact")]
    [SerializeField] private Interactor interactor;     // インタラクトを担当するコンポーネント
    [SerializeField] private ThrowableHolder holder;    // 投擲物の保持・照準を担当するコンポーネント

    [Header("Death")]
    [SerializeField] private PlayerWaterEffect waterEffect;     // 水中での減速や浸水率計算を担当するコンポーネント
    [SerializeField] private float drownThreshold = 0.95f;      // 浸水率がこの値を超えると死亡する
    [SerializeField] private float drownDuration = 3.0f;      // 死亡するまでの秒数

    [Header("UI")]
    [SerializeField] private InventoryUI inventoryUI;   // インベントリUI（Playerの動き止めるのに使う）
    [SerializeField] private PauseManager pauseManager;

    // 状態
    private Rigidbody rb;
    private CapsuleCollider capsule;
    private bool isGrounded;
    private MoveState state = MoveState.Walk;
    private bool staminaExhausted; // 一度切れたら回復するまで走れない
    private Vector2 moveInput;
    private bool jumpRequested;
    private bool inputLocked = false;   // 外部からの入力ロック
    private float footstepTimer;        // 足音SEの再生間隔カウンタ
    private bool drowningSoundPlayed = false;

    // 公開プロパティ（CameraLook が参照）
    public MoveState CurrentState => state;
    public float StaminaRatio => currentStamina / maxStamina;
    public bool IsMoving { get; private set; }

    private bool isDead = false;    // 死亡フラグ
    private float drownTimer = 0f;      // 浸水中の経過時間
    public bool IsDead => isDead;   // 外部から死亡状態を参照するためのプロパティ
    public float DrownProgress => Mathf.Clamp01(drownTimer / drownDuration); // 浸水中の経過時間の割合（0~1）

    // 死亡通知
    public event System.Action OnDeath;

    private float capsuleBottomOffset;  // カプセルコライダーの底面のY座標（ワールド座標）

    // 無敵
    private bool isInvincible = false;
    public bool IsInvincible => isInvincible;

    public void SetInvincible(bool invincible) {
        isInvincible = invincible;
    }

    void Start() {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        // 念のため
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // 初期状態での「足元からCenterまでのオフセット」を記録
        // center.y - height/2 = 底面のPivotからの位置
        capsuleBottomOffset = capsule.center.y - capsule.height * 0.5f;

        currentStamina = maxStamina;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update() {
        if (isDead) return;
        UpdateDrown();

        var kb = Keyboard.current;
        if (kb == null) return;

        // 接地判定
        if (groundCheck != null)
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // 入力を止めるべき状況
        if (inputLocked
            || (pauseManager != null && pauseManager.IsPaused)
            || (inventoryUI != null && inventoryUI.IsOpen)
            || GrabbableDoor.IsAnyGrabbing
            || Cursor.lockState != CursorLockMode.Locked) {
            moveInput = Vector2.zero;
            IsMoving = false;
            return;
        }

        // 入力取得
        Vector2 input = Vector2.zero;
        if (kb.wKey.isPressed) input.y += 1;
        if (kb.sKey.isPressed) input.y -= 1;
        if (kb.dKey.isPressed) input.x += 1;
        if (kb.aKey.isPressed) input.x -= 1;

        bool wantRun = kb.leftShiftKey.isPressed;
        bool wantCrouch = kb.leftCtrlKey.isPressed;
        IsMoving = input.sqrMagnitude > 0.01f;

        // ステート
        if (wantCrouch) state = MoveState.Crouch;
        else if (wantRun && IsMoving && !staminaExhausted) state = MoveState.Run;
        else state = MoveState.Walk;

        bool locked = (interactor != null && interactor.IsInteracting)
                   || (holder != null && holder.IsAiming);
        if (locked) input = Vector2.zero;

        moveInput = input;

        UpdateStamina();
        UpdateCrouchTransition();
        UpdateFootstepSE();

        // ジャンプ入力（フラグ立てるだけ、実処理はFixedUpdate）
        bool canJump = isGrounded
            && state != MoveState.Crouch
            && (waterEffect == null || waterEffect.SubmergeRatio < 0.6f);

        if (kb.spaceKey.wasPressedThisFrame && canJump)
            jumpRequested = true;
    }

    void FixedUpdate() {
        if (isDead) return;

        // 目標速度
        float speed = state switch {
            MoveState.Run => runSpeed,
            MoveState.Crouch => crouchSpeed,
            _ => walkSpeed,
        };
        if (waterEffect != null) speed *= waterEffect.SpeedMultiplier;

        Vector3 wishDir = transform.right * moveInput.x + transform.forward * moveInput.y;
        if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();

        Vector3 targetVel = wishDir * speed;
        Vector3 currentVel = rb.linearVelocity;

        // XZだけ差分を取り、加速度で追従（Yは重力に任せる）
        Vector3 velChange = new Vector3(
            targetVel.x - currentVel.x,
            0f,
            targetVel.z - currentVel.z
        );
        velChange = Vector3.ClampMagnitude(velChange, acceleration * Time.fixedDeltaTime * 10f);
        rb.AddForce(velChange, ForceMode.VelocityChange);

        // ジャンプ
        if (jumpRequested) {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            jumpRequested = false;
        }
    }

    void UpdateStamina() {
        if (state == MoveState.Run) {
            currentStamina -= Time.deltaTime;
            if (currentStamina <= 0f) {
                currentStamina = 0f;
                if (!staminaExhausted) Audio.Post("SE.Player.Breath.OutOfStamina", transform);
                staminaExhausted = true;
            }
        } else {
            float recoverPerSec = maxStamina / recoverDuration;
            currentStamina += recoverPerSec * Time.deltaTime;
            if (currentStamina >= maxStamina) { currentStamina = maxStamina; staminaExhausted = false; }
        }
    }

    void UpdateCrouchTransition() {
        float targetHeight = (state == MoveState.Crouch) ? crouchHeight : standHeight;
        float targetCamY = (state == MoveState.Crouch) ? crouchCameraY : standCameraY;

        capsule.height = Mathf.Lerp(capsule.height, targetHeight, Time.deltaTime * crouchLerpSpeed);
        // 初期のオフセットを保ったままheightに追従
        capsule.center = new Vector3(0f, capsule.height * 0.5f + capsuleBottomOffset, 0f);

        if (cameraTransform != null) {
            Vector3 cp = cameraTransform.localPosition;
            cp.y = Mathf.Lerp(cp.y, targetCamY, Time.deltaTime * crouchLerpSpeed);
            cameraTransform.localPosition = cp;
        }
    }

    /// <summary>移動状態に応じて足音SEを一定間隔で再生する</summary>
    void UpdateFootstepSE() {
        bool onGround = isGrounded && state != MoveState.Crouch;
        if (!onGround || !IsMoving) {
            footstepTimer = 0f;
            return;
        }

        bool underwater = waterEffect != null && waterEffect.IsInWater;
        string key;
        float interval;
        if (underwater) {
            key = "SE.Player.Footstep.WalkUnderwater";
            interval = underwaterStepInterval;
        } else if (state == MoveState.Run) {
            key = "SE.Player.Footstep.Run";
            interval = runStepInterval;
        } else {
            key = "SE.Player.Footstep.Walk";
            interval = walkStepInterval;
        }

        footstepTimer -= Time.deltaTime;
        if (footstepTimer <= 0f) {
            Audio.Post(key, transform);
            footstepTimer = interval;
        }
    }

    void UpdateDrown() {
        if (waterEffect == null) return;
        if (waterEffect.IsInWater && waterEffect.SubmergeRatio >= drownThreshold) {
            // 溺れ始めた最初の1回だけ再生
            if (!drowningSoundPlayed) {
                Audio.Post("SE.Player.Breath.Drowning", transform);
                drowningSoundPlayed = true;
            }

            drownTimer += Time.deltaTime;

            if (drownTimer >= drownDuration && !isInvincible) {
                Die();
            }
        } else {
            // 閾値を下回ったらタイマーリセット（顔が水面より上に出れば助かる）
            drownTimer = Mathf.Max(0f, drownTimer - Time.deltaTime * 2f); // 回復は少し早め

            // 完全に回復したらフラグリセット（再度溺れたら鳴らせる）
            if (drownTimer <= 0f) {
                drowningSoundPlayed = false;
            }
        }
    }

    void Die() {
        isDead = true;
        Debug.Log($"プレイヤーは死亡した");
        OnDeath?.Invoke();
    }

    /// <summary>外部から殺す（敵の攻撃など）</summary>
    public void Kill() {
        if (isDead) return;
        if (isInvincible) return;
        Die();
    }

    /// <summary>外部から入力をロック/解除する</summary>
    public void SetInputLocked(bool locked) {
        inputLocked = locked;
        if (locked) {
            moveInput = Vector2.zero;
            IsMoving = false;
        }
    }
}
