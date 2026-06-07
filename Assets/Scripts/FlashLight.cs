using UnityEngine;

public class Flashlight : MonoBehaviour {
    [Header("References")]
    [SerializeField] private Light flashlight;
    [SerializeField] private Transform followTarget; // í èÌÇÕCamera

    void Update() {
        // ÉJÉÅÉâí«è]
        if (followTarget != null) {
            transform.position = followTarget.position;
            transform.rotation = followTarget.rotation;
        }
    }
}
