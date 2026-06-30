// テスト音発生装置　適当なObjectにつけて
// Tキー　押せばなる

using UnityEngine;
using UnityEngine.InputSystem;

public class TestSoundEmitter : MonoBehaviour {
    void Update() {
        if (Keyboard.current.tKey.wasPressedThisFrame) {
            SoundSystem.Emit(new SoundInfo {
                position = transform.position,
                loudness = 1f,
                type = SoundType.Impact,
                source = gameObject
            });
            Debug.Log("Test sound emitted at " + transform.position);
        }
    }
}
