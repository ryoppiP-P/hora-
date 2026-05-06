using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Throwable : Interactable {
    [SerializeField] private string displayName = "空き缶";

    // 着地イベント（音システムが購読する想定）
    [System.Serializable] public class LandEvent : UnityEvent<Throwable, Collision> { }
    public LandEvent OnLanded;

    private Rigidbody rb;
    private Collider col;
    private bool isHeld;

    public override string PromptText => $"{displayName} を拾う";

    void Awake() {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public override void OnInteractComplete(Player player) {
        var holder = player.GetComponent<ThrowableHolder>();
        if (holder != null) holder.Pickup(this);
    }

    public void SetHeld(bool held, Transform parent = null) {
        isHeld = held;
        rb.isKinematic = held;
        col.enabled = !held;

        if (held && parent != null) {
            transform.SetParent(parent, worldPositionStays: false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
        else {
            transform.SetParent(null, worldPositionStays: true);
        }
    }

    public void Throw(Vector3 velocity) {
        SetHeld(false);
        rb.linearVelocity = velocity;        // Unity 6 以降の名前
        // Unity 2022 以前は rb.velocity = velocity;
        rb.angularVelocity = Random.insideUnitSphere * 5f;
    }

    void OnCollisionEnter(Collision collision) {
        if (isHeld) return;
        OnLanded?.Invoke(this, collision);
    }
}
