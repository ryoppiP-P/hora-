using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class GrabbableDoor : Interactable {
    [SerializeField] private Camera cam;
    [SerializeField] private Interactor interactor;
    [SerializeField] private float sensitivity = 2f;   // マウス感度
    [SerializeField] private float openSpeed = 200f;   // ドアの最大回転速度(度/秒)
    [SerializeField] private float smoothTime = 0.12f; // 滑らかさ（大きいほどゆっくり加減速する）

    [Header("開放音（ドアが動いている間だけ鳴る。速さで音量・再生速度・ボスに届く距離が変わる）")]
    [SerializeField] private float referenceAngularSpeed = 150f;      // 度/秒。この速さで最大音量になる
    [SerializeField] private float pitchReferenceAngularSpeed = 60f;  // 度/秒。この速さでクリップが等速(ピッチ1.0)で再生される
    [SerializeField] private float minCreakPitch = 0.2f;              // 遅い時のピッチ下限（低すぎるとノイズになるため）
    [SerializeField] private float maxCreakPitch = 1.0f;              // 速い時のピッチ上限
    [SerializeField] private float minCreakVolume = 0.35f;
    [SerializeField] private float maxCreakVolume = 1.0f;
    [SerializeField] private float minCreakLoudness = 0.2f; // ボスへ知らせる音の大きさ(0～1)の下限
    [SerializeField] private float maxCreakLoudness = 1.0f; // ボスへ知らせる音の大きさ(0～1)の上限
    [SerializeField] private float soundEmitInterval = 0.25f; // ボスへの通知を送る間隔(秒)

    [Header("旋回")]
    [SerializeField] private float minAngle = -95f;    // 半開状態からの最小角度
    [SerializeField] private float maxAngle = 95f;     // 半開状態からの最大角度

    [Header("Lock")]
    [SerializeField] private bool isLocked = false;     // 鍵がかかっているか
    [SerializeField] private ItemBase requiredKey;      // 必要な鍵アイテム
    [SerializeField] private bool consumeKey = true;    // 使ったら消費するか
    [SerializeField] private InventoryUI inventoryUI;   // インベントリUI
    [SerializeField] private Inventory inventory;       // インベントリ
    [SerializeField] private GrabbableDoor[] linkedDoors; // 連動して解錠される他のドア（同じ鍵を使う場合など）

    public override string PromptText => isLocked ? "鍵がかかっている" : "掴んで開く";
    public bool IsLocked => isLocked;

    // 「動いている」とみなす最低角速度(度/秒)。この付近で音量が滑らかに0へフェードする
    private const float MovingThreshold = 6f;
    // 角速度の平滑化の速さ
    private const float SpeedSmoothing = 12f;

    private Rigidbody rb;
    private BoxCollider box;
    private Collider playerCollider; // 開いている間だけ物理的な押し出しを止める相手
    private bool isGrabbed;
    private float currentAngle;   // 半開状態からの現在角度
    private float targetAngle;    // マウス操作で決まる目標角度
    private float angleVelocity;  // SmoothDamp用の内部速度
    private AudioHandle creakHandle; // AudioManager経由で再生中のきしみ音

    private float lastFixedAngularSpeed; // FixedUpdateで計測した実際の角速度(度/秒)
    private float smoothedAngularSpeed;  // それを平滑化したもの
    private float soundEmitTimer;        // ボスへの次回通知までの残り時間

    private Quaternion closedLocalRot;

    // 最初から食い込んでいる相手（他のドアや枠など）
    private readonly HashSet<Collider> ignoredColliders = new HashSet<Collider>();

    public static bool IsAnyGrabbing { get; private set; }

    void Awake() {
        rb = GetComponent<Rigidbody>();
        box = GetComponent<BoxCollider>();
        if (cam == null) cam = Camera.main;

        closedLocalRot = transform.localRotation;

        // 手動操作をKinematicで実現する。
        // HingeJointのモーターで回す方式は、扉やプレイヤーと食い込んだ時に
        // PhysXが強い反発力を出してしまい、位置がNaNになるなど
        // プレイヤーが開ききるまで扉が引っかかることが多かったため、
        // 物理の力を借りずスクリプトだけで直接姿勢を与える方式にしている。
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        if (interactor != null) playerCollider = interactor.GetComponent<Collider>();
    }

    void Start() {
        IgnoreInitialOverlaps();
    }

    /// <summary>半開状態で既に食い込んでいる相手との当たり判定を無効化する</summary>
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
                lastFixedAngularSpeed = 0f;
                smoothedAngularSpeed = 0f;
                soundEmitTimer = 0f;

                // きしみ音は掴んでいる間ずっとループ再生しておき、
                // 実際に聞こえるかどうかは毎フレームの音量とピッチで制御する
                creakHandle = Audio.Post("SE.Player.Door.Hinged.Open", transform.position);
                if (creakHandle != null) {
                    // 開始位置だけは現在の開き具合に合わせておく
                    // （以降はピッチで進める。毎フレームのシークはブツ切れの原因になるのでしない）
                    creakHandle.SetVolume(0f);
                    creakHandle.SetProgress(CalculateOpenProgress());
                }

                // 開いている間中にプレイヤー自身の当たり判定へ食い込むと
                // 開き切ろうとするのを防ぐため、掴んでいる間だけ衝突を無視する
                if (playerCollider != null) Physics.IgnoreCollision(box, playerCollider, true);
            }
        }
        if (mouse.leftButton.wasReleasedThisFrame) Release();

        if (!isGrabbed) return;

        // マウスの横移動量を目標角度に加算（多少先に決める）
        float dx = mouse.delta.ReadValue().x;
        if (!float.IsNaN(dx) && !float.IsInfinity(dx)) {
            targetAngle = Mathf.Clamp(targetAngle + dx * sensitivity, minAngle, maxAngle);
        }

        UpdateCreakSound();
    }

    /// <summary>ドアの開き具合を0～1で返す</summary>
    float CalculateOpenProgress() {
        float openRange = Mathf.Max(Mathf.Abs(minAngle), Mathf.Abs(maxAngle), 0.01f);
        return Mathf.Clamp01(Mathf.Abs(currentAngle) / openRange);
    }

    /// <summary>
    /// ドアが「実際に」動いている速さから、きしみ音の音量・再生速度と
    /// ボスへ届く音の大きさを決める。
    /// 再生位置のシークは行わず、再生速度(ピッチ)でクリップを進めることで
    /// 「再生位置＝ドアの開き具合」を音を途切れさせずに成立させる。
    /// </summary>
    void UpdateCreakSound() {
        // FixedUpdateで計測した実際の角速度を平滑化する
        smoothedAngularSpeed = Mathf.Lerp(
            smoothedAngularSpeed,
            lastFixedAngularSpeed,
            1f - Mathf.Exp(-SpeedSmoothing * Time.deltaTime));

        // 何らかの理由でボイスが解放されてしまった場合に備えて再取得する
        if (creakHandle == null || !creakHandle.IsPlaying) {
            creakHandle = Audio.Post("SE.Player.Door.Hinged.Open", transform.position);
            if (creakHandle != null) creakHandle.SetProgress(CalculateOpenProgress());
        }

        float speedFactor = Mathf.Clamp01(smoothedAngularSpeed / referenceAngularSpeed);
        // 止まる寸前で音量が0/最小の間をバタつかないよう、しきい値付近は滑らかにフェードさせる
        float movingFade = Mathf.Clamp01(smoothedAngularSpeed / MovingThreshold);

        if (creakHandle != null) {
            creakHandle.SetVolume(Mathf.Lerp(minCreakVolume, maxCreakVolume, speedFactor) * movingFade);

            // ドアの動く速さに再生速度を合わせる。
            // pitchReferenceAngularSpeedで開けた時にクリップが等速で流れる
            float pitch = Mathf.Clamp(
                smoothedAngularSpeed / Mathf.Max(pitchReferenceAngularSpeed, 0.01f),
                minCreakPitch,
                maxCreakPitch);
            creakHandle.SetPitch(pitch);
        }

        // ボスAIへの通知も、実際に動いている時だけ・速さに応じた大きさで送る
        soundEmitTimer -= Time.deltaTime;
        if (smoothedAngularSpeed >= MovingThreshold && soundEmitTimer <= 0f) {
            soundEmitTimer = soundEmitInterval;
            SoundSystem.Emit(new SoundInfo {
                position = transform.position,
                loudness = Mathf.Lerp(minCreakLoudness, maxCreakLoudness, speedFactor),
                type = SoundType.Gimmick,
                source = gameObject
            });
        }
    }

    void FixedUpdate() {
        if (!isGrabbed) return;

        // 一定速度で動かすMoveTowardsではなく、加減速がつくSmoothDampでなめらかに動かす
        // （openSpeedはmaxSpeedとして上限速度に使う）
        float next = Mathf.SmoothDamp(currentAngle, targetAngle, ref angleVelocity, smoothTime, openSpeed, Time.fixedDeltaTime);

        if (Mathf.Approximately(next, currentAngle)) {
            lastFixedAngularSpeed = 0f;
            return;
        }

        // 壁などに当たる角度へは進めない
        if (IsBlocked(next)) {
            targetAngle = currentAngle;
            angleVelocity = 0f;
            lastFixedAngularSpeed = 0f;
            return;
        }

        // 音量・ピッチに使う実際の角速度はここで計測する
        // （Updateで測るとFixedUpdateが走らないフレームで0になり、音がちらつくため）
        lastFixedAngularSpeed = Mathf.Abs(next - currentAngle) / Time.fixedDeltaTime;

        currentAngle = next;
        rb.MoveRotation(WorldRotationAt(currentAngle));
    }

    /// <summary>指定角度でのワールド回転（扉の端のTransform原点を基準に加算）</summary>
    Quaternion WorldRotationAt(float angle) {
        Quaternion parentRot = transform.parent != null ? transform.parent.rotation : Quaternion.identity;
        return parentRot * closedLocalRot * Quaternion.Euler(0f, angle, 0f);
    }

    /// <summary>その角度に動く際に壁にぶつかるか</summary>
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
            if (other.attachedRigidbody != null) continue;   // プレイヤー自体は加味しない
            if (ignoredColliders.Contains(other)) continue;  // 初期から食い込んでいる枠
            return true;
        }
        return false;
    }

    public bool TryUnlock(ItemBase item) {
        if (!isLocked) return false;
        if (item == requiredKey) {
            UnlockInternal(true);
            if (consumeKey && inventory != null) inventory.RemoveOne(item);
            Debug.Log($"{name} 解錠");
            Audio.Post("SE.Player.Door.Key.Unlock", transform.position);
            return true;
        }
        Audio.Post("SE.Player.Console.UnlockError");
        Debug.Log("そのアイテムじゃ、開かない！");
        return false;
    }

    // 連動解錠用（linkedDoorsの無限ループ防止にpropagateフラグ付き、AutoDoorと同じ発想）
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
        if (creakHandle != null) creakHandle.Stop();
        creakHandle = null;
        angleVelocity = 0f;
        lastFixedAngularSpeed = 0f;
        smoothedAngularSpeed = 0f;
    }

    void OnDisable() {
        if (isGrabbed) {
            isGrabbed = false;
            IsAnyGrabbing = false;
            if (playerCollider != null) Physics.IgnoreCollision(box, playerCollider, false);
        }
        if (creakHandle != null) creakHandle.Stop();
        creakHandle = null;
        angleVelocity = 0f;
        lastFixedAngularSpeed = 0f;
        smoothedAngularSpeed = 0f;
    }
}
