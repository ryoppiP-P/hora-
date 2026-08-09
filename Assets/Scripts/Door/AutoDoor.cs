// 自動開閉ドア
using UnityEngine;

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

    private Vector3 upperClosedPos;
    private Vector3 lowerClosedPos;
    private Vector3 upperOpenPos;
    private Vector3 lowerOpenPos;

    private bool isOpen = false;
    private bool isMoving = false;

    public bool IsOpen => isOpen;

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

    // 連動処理用（連鎖の無限ループ防止のためpropagateフラグ付き）
    private void OpenInternal(bool propagate) {
        if (isOpen) return;
        isOpen = true;
        isMoving = true;

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
