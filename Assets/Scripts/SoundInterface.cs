using UnityEngine;

public enum SoundType {
    Footstep,   // 足音
    Throw,      // 投擲物の発射音
    Impact,     // 物の衝突音
    Gimmick,     // ギミックの動作音
}

public struct SoundInfo {
    public Vector3 position;   // 音の発生位置（ワールド座標）
    public float loudness;     // 音量（0.0 ~ 1.0）
    public SoundType type;     // 音の種類
    public GameObject source;  // 音を出したオブジェクト
}

public static class SoundSystem {
    public static event System.Action<SoundInfo> OnSound;

    public static void Emit(SoundInfo info) {
        OnSound?.Invoke(info);
    }
}