using UnityEngine;

public abstract class Interactable : MonoBehaviour {
    [SerializeField] protected float holdDuration = 1f; // E長押し必要時間（瞬時拾得なら0）
    public float HoldDuration => holdDuration;

    public virtual string PromptText => "操作する";

    // 長押し開始・進行・完了・キャンセル
    public virtual void OnInteractStart(Player player) { }
    public virtual void OnInteractProgress(Player player, float t) { } // t: 0-1
    public virtual void OnInteractComplete(Player player) { }
    public virtual void OnInteractCancel(Player player) { }
}
