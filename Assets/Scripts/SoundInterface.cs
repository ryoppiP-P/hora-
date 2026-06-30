using UnityEngine;
using UnityEngine.AI;

// ============================================
// 音の種類
// ============================================
public enum SoundType {
    Footstep,   // 足音
    Throw,      // 投擲物の発射音
    Impact,     // 物の衝突音
    Gimmick,    // ギミックの動作音
}

// ============================================
// 音の情報（発信側がEmit時に詰めるデータ）
// ============================================
public struct SoundInfo {
    public Vector3 position;   // 音の発生位置（ワールド座標）
    public float loudness;     // 音量（0.0 ~ 1.0）
    public SoundType type;     // 音の種類
    public GameObject source;  // 音を出したオブジェクト
}

// ============================================
// 音の中継局（Pub/Sub）
// 発信側: SoundSystem.Emit(SoundInfo) を呼ぶ
// 受信側: SoundSystem.OnSound += handler で購読
// ============================================
public static class SoundSystem {
    public static event System.Action<SoundInfo> OnSound;

    public static void Emit(SoundInfo info) {
        OnSound?.Invoke(info);
    }
}

// ============================================
// 音響伝播ユーティリティ
// 聴覚を持つオブジェクト（敵など）が利用する
// NavMesh経路に沿って音が伝わるロジックを集約
// ============================================
public static class SoundPropagation {
    /// <summary>
    /// 聴き手が音源の音を知覚できるかを計算する。
    /// NavMesh経路距離で減衰を計算し、聞こえる方向は経路の最初の角を返す。
    /// </summary>
    /// <param name="listenerPos">聴き手の位置</param>
    /// <param name="sourcePos">音源の位置</param>
    /// <param name="loudness">元の音量（0~1）</param>
    /// <param name="maxDistance">この距離で音量0になる</param>
    /// <param name="perceived">減衰後の音量（出力）</param>
    /// <param name="directionTarget">音が聞こえてくる方向の目標地点（出力、敵の移動先に使える）</param>
    /// <returns>経路が存在すれば true、到達不能なら false</returns>
    public static bool TryHear(
        Vector3 listenerPos,
        Vector3 sourcePos,
        float loudness,
        float maxDistance,
        out float perceived,
        out Vector3 directionTarget) {
        perceived = 0f;
        directionTarget = sourcePos;

        var path = new NavMeshPath();
        if (!NavMesh.CalculatePath(listenerPos, sourcePos, NavMesh.AllAreas, path))
            return false;
        if (path.status != NavMeshPathStatus.PathComplete)
            return false;

        // 経路の総距離
        float pathLength = 0f;
        for (int i = 0; i < path.corners.Length - 1; i++)
            pathLength += Vector3.Distance(path.corners[i], path.corners[i + 1]);

        // 距離による減衰（2乗カーブで逆二乗則っぽく）
        float t = Mathf.Clamp01(1f - pathLength / maxDistance);
        perceived = loudness * t * t;

        // 音が聞こえてくる方向（経路上の最初の曲がり角）
        directionTarget = path.corners.Length >= 2 ? path.corners[1] : sourcePos;

        return true;
    }
}
