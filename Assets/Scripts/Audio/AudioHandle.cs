//==============================================================================
// 作成日: 2026/07/28
// 作成者: 岩崎瑛斗
// 概要: 再生中のSEを安全に停止・確認するためのハンドル
//==============================================================================

/// <summary>
/// Audio.Postで開始した音を後から操作するためのハンドル。
/// プールが再利用されても別の音を誤って停止しないよう、再生IDで識別する。
/// </summary>
public sealed class AudioHandle
{
    private readonly AudioManager manager;
    private readonly int playbackId;

    internal AudioHandle(AudioManager manager, int playbackId)
    {
        this.manager = manager;
        this.playbackId = playbackId;
    }

    /// <summary>
    /// このハンドルに対応する音が現在再生中か。
    /// </summary>
    public bool IsPlaying => manager != null && manager.IsPlaying(playbackId);

    /// <summary>
    /// 音を停止する。fadeTimeが0より大きい場合は音量を徐々に下げて停止する。
    /// </summary>
    public void Stop(float fadeTime = 0f)
    {
        if (manager != null)
            manager.Stop(playbackId, fadeTime);
    }
}
