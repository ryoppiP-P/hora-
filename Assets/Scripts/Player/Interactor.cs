using UnityEngine;
using UnityEngine.InputSystem;

public class Interactor : MonoBehaviour {
    [SerializeField] private Camera cam;
    [SerializeField] private Player player;
    [SerializeField] private float rayDistance = 2.0f;
    [SerializeField] private LayerMask interactMask = ~0;

    [Header("UI (任意)")]
    [SerializeField] private InteractUI ui;

    private Interactable currentTarget;
    private float holdTimer;
    private bool isHolding;

    public bool IsInteracting => isHolding;

    void Update() {
        var kb = Keyboard.current;
        if (kb == null) return;

        // 視線レイ
        Interactable hit = RaycastInteractable();

        // ホールド中は対象を固定（途中で外れたらキャンセル）
        if (!isHolding) currentTarget = hit;

        // 入力
        bool ePressed = kb.eKey.wasPressedThisFrame;
        bool eHeld = kb.eKey.isPressed;
        bool eReleased = kb.eKey.wasReleasedThisFrame;

        // 開始
        if (ePressed && currentTarget != null) {
            isHolding = true;
            holdTimer = 0f;
            currentTarget.OnInteractStart(player);
        }

        // 進行
        if (isHolding && eHeld && currentTarget != null) {
            // 視線が外れたらキャンセル
            if (hit != currentTarget) {
                Cancel();
            }
            else {
                holdTimer += Time.deltaTime;
                float t = (currentTarget.HoldDuration <= 0f)
                    ? 1f
                    : Mathf.Clamp01(holdTimer / currentTarget.HoldDuration);
                currentTarget.OnInteractProgress(player, t);

                if (t >= 1f) {
                    currentTarget.OnInteractComplete(player);
                    isHolding = false;
                    holdTimer = 0f;
                    currentTarget = null;
                }
            }
        }

        // E離した（未完了ならキャンセル）
        if (eReleased && isHolding) {
            Cancel();
        }

        // UI更新
        if (ui != null)
            ui.UpdateUI(currentTarget, isHolding ? holdTimer : 0f, currentTarget?.HoldDuration ?? 0f);
    }

    Interactable RaycastInteractable() {
        if (cam == null) return null;
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out var info, rayDistance, interactMask, QueryTriggerInteraction.Collide)) {
            return info.collider.GetComponentInParent<Interactable>();
        }
        return null;
    }

    void Cancel() {
        if (currentTarget != null) currentTarget.OnInteractCancel(player);
        isHolding = false;
        holdTimer = 0f;
        currentTarget = null;
    }
}
