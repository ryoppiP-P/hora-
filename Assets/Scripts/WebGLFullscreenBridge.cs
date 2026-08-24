using UnityEngine;
using System.Runtime.InteropServices;

// ブラウザのfullscreenchangeイベントを直接拾うブリッジ。
// UnityRoomでフルスクリーン中にEscapeを押すと、ブラウザがそのキー入力を
// フルスクリーン解除に横取りしてしまい、Unity側にkeydownイベントが
// 届かない/遅れることがある。Cursor.lockStateの毎フレームポーリングは
// Inventory/Note等の他スクリプトの一時的な状態変化と衝突して誤爆したため、
// フルスクリーン専用のブラウザネイティブイベントだけを狙って拾う。
public class WebGLFullscreenBridge : MonoBehaviour {
    [SerializeField] private PauseManager pauseManager;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void RegisterFullscreenExitListener(string gameObjectName);
#endif

    void Start() {
#if UNITY_WEBGL && !UNITY_EDITOR
        RegisterFullscreenExitListener(gameObject.name);
#endif
    }

    // JS側のfullscreenchangeイベントからSendMessageで呼ばれる
    public void OnBrowserFullscreenExited() {
        if (pauseManager != null) pauseManager.OnBrowserFullscreenExited();
    }
}
