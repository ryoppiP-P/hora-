//==============================================================================
// 作成日: 2026/07/28
// 作成者: 岩崎瑛斗
// 概要: ゲーム側から文字列IDでサウンドを再生するための公開API
//==============================================================================

using UnityEngine;

/// <summary>
/// サウンド再生を簡単に呼び出すための静的ラッパー。
/// AIの聴覚通知とは接続せず、実際の音声再生だけを担当する。
/// </summary>
public static class Audio
{
    /// <summary>
    /// 2DのSEを再生する。
    /// </summary>
    public static AudioHandle Post(string key)
    {
        return AudioManager.GetOrCreate().Post(key, null, Vector3.zero, false);
    }

    /// <summary>
    /// 指定したワールド座標からSEを再生する。
    /// AudioDataが2D設定の場合、座標は使用されない。
    /// </summary>
    public static AudioHandle Post(string key, Vector3 position)
    {
        return AudioManager.GetOrCreate().Post(key, null, position, true);
    }

    /// <summary>
    /// 指定したTransformを追従しながらSEを再生する。
    /// </summary>
    public static AudioHandle Post(string key, Transform followTarget)
    {
        if (followTarget == null)
        {
            Debug.LogWarning($"[Audio] 追従先がnullです。2D再生へ切り替えます: {key}");
            return Post(key);
        }

        return AudioManager.GetOrCreate().Post(
            key,
            followTarget,
            followTarget.position,
            true);
    }

    /// <summary>
    /// BGMを再生する。別のBGMが鳴っている場合はクロスフェードする。
    /// </summary>
    public static void PlayBGM(string key, float fadeTime = 1f)
    {
        AudioManager.GetOrCreate().PlayBGM(key, fadeTime);
    }

    /// <summary>
    /// 現在のBGMをフェードアウトして停止する。
    /// </summary>
    public static void StopBGM(float fadeTime = 1f)
    {
        AudioManager.GetOrCreate().StopBGM(fadeTime);
    }
}
