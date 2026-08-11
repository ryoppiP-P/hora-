using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Throwable : MonoBehaviour {
    [SerializeField] private string displayName = "空き缶";

    // 投げた後に地面に着地した時のイベント
    [System.Serializable] public class LandEvent : UnityEvent<Throwable, Collision> { }
    public LandEvent OnLanded;

    private Rigidbody rb;
    private Collider col;
    private bool isHeld;

    public string DisplayName => displayName;

    void Awake() {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public void SetHeld(bool held, Transform parent = null) {
        isHeld = held;
        rb.isKinematic = held;
        col.enabled = !held;

        if (held && parent != null) {
            transform.SetParent(parent, worldPositionStays: false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        } else {
            transform.SetParent(null, worldPositionStays: true);
        }
    }

    public void Throw(Vector3 velocity) {
        SetHeld(false);
        rb.linearVelocity = velocity;
        rb.angularVelocity = Random.insideUnitSphere * 5f;
    }

    void OnCollisionEnter(Collision collision) {
        if (isHeld) return;
        Audio.Post("SE.Player.Can.Impact", transform.position);
        OnLanded?.Invoke(this, collision);
    }
}
