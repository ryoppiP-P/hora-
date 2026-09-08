// 自動開閉ドア
using UnityEngine;
using UnityEngine.AI;

public class AutoDoor : MonoBehaviour {
    [Header("Door Parts")]
    [SerializeField] private Transform upperPart;   // 上に開くパーツ
    [SerializeField] private Transform lowerPart;   // 下に開くパーツ

    [Header("Open Settings")]
    [SerializeField] private float openAmount = 1.3f;   // 開く量（片側）
    [SerializeField] private float openSpeed = 1.0f;    // 1秒あたりの移動量
    [SerializeField] private bool openOnStart = false;  // ゲーム開始時に開いてる状態にするか

    [Header("Linked Doors (連動する他のドア)")]
    [SerializeField] private AutoDoor[] linkedDoors;

    [Header("Lock (Bossの通行制御用。AutoDoorLockと同期される)")]
    [SerializeField] private bool isLocked = false;      // true中はBossの経路から除外する

    [Header("Boss通行止めの範囲 (シーンビューの赤い箱)")]
    [SerializeField] private Vector3 bossBlockCenter = Vector3.zero; // ローカル座標での中心
    [SerializeField] private Vector3 bossBlockSize = Vector3.zero;   // ローカル座標でのサイズ（0なら実行時にドアから自動計算）
    [SerializeField] private float bossBlockMinThickness = 1f;   // 薄い軸をこの厚みまで太らせる（NavMeshのボクセル0.17より薄いと穴が開かない）
    [SerializeField] private float bossBlockWidthPadding = 0.3f; // 幅方向に持たせる余裕
    [SerializeField] private float bossBlockHeightPadding = 0.5f; // 上下に持たせる余裕（床のNavMeshを確実に貫くため）

    private Vector3 upperClosedPos;
    private Vector3 lowerClosedPos;
    private Vector3 upperOpenPos;
    private Vector3 lowerOpenPos;

    private bool isOpen = false;
    private bool isMoving = false;
    private NavMeshObstacle navObstacle;

    public bool IsOpen => isOpen;
    public bool IsLocked => isLocked;

    void Awake() {
        // ロック中はNavMeshObstacleでBossの経路から除外する（鍵/スイッチが開いたら自動的に解除）
        navObstacle = GetComponent<NavMeshObstacle>();
        if (navObstacle == null) navObstacle = gameObject.AddComponent<NavMeshObstacle>();
        navObstacle.shape = NavMeshObstacleShape.Box;
        navObstacle.carveOnlyStationary = false;
        navObstacle.carving = true;

        // Inspectorで範囲が入っていなければドアの実寸から求める
        if (bossBlockSize.sqrMagnitude < 0.0001f) FitBossBlockToDoor();

        navObstacle.center = bossBlockCenter;
        navObstacle.size = bossBlockSize;
        UpdateBossBlock();
    }

    /// <summary>
    /// upperPart/lowerPartの実コライダーからドア口の範囲を求め、bossBlockCenter/bossBlockSizeへ入れ直す。
    /// AutoDoorのTransform自体はスイッチ側にあり、しかも90度単位で回っているので、
    /// ワールドのサイズをそのまま使うと位置も向きもズレる。頂点をローカル空間へ変換して求めること。
    /// Inspectorの歯車メニューから手動でも実行できる。
    /// </summary>
    [ContextMenu("Boss通行止めの範囲をドアに合わせる")]
    public void FitBossBlockToDoor() {
        Collider upperCol = upperPart != null ? upperPart.GetComponent<Collider>() : null;
        Collider lowerCol = lowerPart != null ? lowerPart.GetComponent<Collider>() : null;

        bool any = false;
        Vector3 min = Vector3.zero;
        Vector3 max = Vector3.zero;

        if (upperCol != null) IncludeLocalBounds(upperCol, ref min, ref max, ref any);
        if (lowerCol != null) IncludeLocalBounds(lowerCol, ref min, ref max, ref any);
        if (!any) return;

        Vector3 center = (min + max) * 0.5f;
        Vector3 size = max - min;

        // ドア板は薄すぎてNavMeshに穴が開かないので、薄い方の水平軸を最低厚みまで太らせる
        if (size.x < size.z) {
            size.x = Mathf.Max(size.x, bossBlockMinThickness);
            size.z += bossBlockWidthPadding;
        } else {
            size.z = Mathf.Max(size.z, bossBlockMinThickness);
            size.x += bossBlockWidthPadding;
        }

        // 床のNavMeshを確実に貫くよう、上下にも余裕を持たせる
        size.y += bossBlockHeightPadding * 2f;

        bossBlockCenter = center;
        bossBlockSize = size;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    // コライダーの箱の8頂点をこのTransformのローカル空間へ変換してmin/maxに含める
    void IncludeLocalBounds(Collider col, ref Vector3 min, ref Vector3 max, ref bool any) {
        BoxCollider boxCol = col as BoxCollider;
        Vector3 localCenter;
        Vector3 localExtents;
        Matrix4x4 toWorld;

        if (boxCol != null) {
            // コライダー自身のローカル箱を使う（回転していても正確に求まる）
            localCenter = boxCol.center;
            localExtents = boxCol.size * 0.5f;
            toWorld = boxCol.transform.localToWorldMatrix;
        } else {
            Bounds b = col.bounds;
            localCenter = b.center;
            localExtents = b.extents;
            toWorld = Matrix4x4.identity;
        }

        for (int xi = -1; xi <= 1; xi += 2) {
            for (int yi = -1; yi <= 1; yi += 2) {
                for (int zi = -1; zi <= 1; zi += 2) {
                    Vector3 corner = localCenter + new Vector3(localExtents.x * xi, localExtents.y * yi, localExtents.z * zi);
                    Vector3 local = transform.InverseTransformPoint(toWorld.MultiplyPoint3x4(corner));
                    if (!any) {
                        min = local;
                        max = local;
                        any = true;
                    } else {
                        min = Vector3.Min(min, local);
                        max = Vector3.Max(max, local);
                    }
                }
            }
        }
    }

    // ロック中のドアは、Bossが通れない範囲を赤い箱でシーンビューに表示する
    void OnDrawGizmos() {
        if (!isLocked) return;
        if (bossBlockSize.sqrMagnitude < 0.0001f) return;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.25f);
        Gizmos.DrawCube(bossBlockCenter, bossBlockSize);
        Gizmos.color = new Color(1f, 0.15f, 0.15f, 1f);
        Gizmos.DrawWireCube(bossBlockCenter, bossBlockSize);
        Gizmos.matrix = Matrix4x4.identity;
    }

    void Start() {
        // 閉じている状態の位置を記録
        if (upperPart != null) {
            upperClosedPos = upperPart.localPosition;
            upperOpenPos = upperClosedPos + Vector3.up * openAmount;
        }
        if (lowerPart != null) {
            lowerClosedPos = lowerPart.localPosition;
            lowerOpenPos = lowerClosedPos + Vector3.down * openAmount;
        }

        if (openOnStart) {
            if (upperPart != null) upperPart.localPosition = upperOpenPos;
            if (lowerPart != null) lowerPart.localPosition = lowerOpenPos;
            isOpen = true;
        }
    }

    void Update() {
        if (!isMoving) return;

        Vector3 targetUpper = isOpen ? upperOpenPos : upperClosedPos;
        Vector3 targetLower = isOpen ? lowerOpenPos : lowerClosedPos;

        bool upperDone = true, lowerDone = true;

        if (upperPart != null) {
            upperPart.localPosition = Vector3.MoveTowards(upperPart.localPosition, targetUpper, openSpeed * Time.deltaTime);
            upperDone = upperPart.localPosition == targetUpper;
        }

        if (lowerPart != null) {
            lowerPart.localPosition = Vector3.MoveTowards(lowerPart.localPosition, targetLower, openSpeed * Time.deltaTime);
            lowerDone = lowerPart.localPosition == targetLower;
        }

        if (upperDone && lowerDone) isMoving = false;
    }

    /// <summary>外部から開く指示（鍵解錠時などに呼ぶ）</summary>
    public void Open() {
        OpenInternal(true);
    }

    /// <summary>外部から閉じる指示</summary>
    public void Close() {
        CloseInternal(true);
    }

    /// <summary>開閉トグル</summary>
    public void Toggle() {
        if (isOpen) Close(); else Open();
    }

    /// <summary>Bossが鍵のかかっていない自動ドアに近づいた時に開かせる</summary>
    public void BossForceOpen() {
        if (isLocked) return;
        Open();
    }

    /// <summary>鍵の状態に応じてBoss用のNavMeshObstacleを切り替える</summary>
    void UpdateBossBlock() {
        if (navObstacle != null) navObstacle.enabled = isLocked;
    }

    /// <summary>AutoDoorLockから呼ばれる：鍵の状態を同期する</summary>
    public void SetLocked(bool locked) => SetLockedInternal(locked, true);

    void SetLockedInternal(bool locked, bool propagate) {
        isLocked = locked;
        UpdateBossBlock();

        if (propagate && linkedDoors != null) {
            foreach (var d in linkedDoors) {
                if (d != null && d != this) d.SetLockedInternal(locked, false);
            }
        }
    }

    // 連動処理用（連鎖の無限ループ防止のためpropagateフラグ付き）
    private void OpenInternal(bool propagate) {
        if (isOpen) return;
        isOpen = true;
        isMoving = true;
        Audio.Post("SE.Player.Door.AutomaticLarge.Open", transform.position);
        // ボスAIにドアの開放音を知らせる
        SoundSystem.Emit(new SoundInfo {
            position = transform.position,
            loudness = 0.7f,
            type = SoundType.Gimmick,
            source = gameObject
        });

        if (propagate && linkedDoors != null) {
            foreach (var d in linkedDoors) {
                if (d != null && d != this) d.OpenInternal(false);
            }
        }
    }

    private void CloseInternal(bool propagate) {
        if (!isOpen) return;
        isOpen = false;
        isMoving = true;

        if (propagate && linkedDoors != null) {
            foreach (var d in linkedDoors) {
                if (d != null && d != this) d.CloseInternal(false);
            }
        }
    }
}
