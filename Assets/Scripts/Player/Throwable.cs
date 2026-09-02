using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Throwable : MonoBehaviour {
    [SerializeField] private string displayName = "�󂫊�";

    // ��������ɒn�ʂɒ��n�������̃C�x���g
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
        // ボスAIに着弾音を知らせる（おびき寄せ/そらすギミックとして機能させるため）
        SoundSystem.Emit(new SoundInfo {
            position = transform.position,
            loudness = 1.0f,
            type = SoundType.Impact,
            source = gameObject
        });
        OnLanded?.Invoke(this, collision);
    }
}
