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

    /// <summary>
    /// 音量を0～1で直接設定する。
    /// </summary>
    public void SetVolume(float normalized01)
    {
        if (manager != null)
            manager.SetVolume(playbackId, normalized01);
    }

    /// <summary>
    /// 再生速度(ピッチ)を設定する。
    /// </summary>
    public void SetPitch(float pitch)
    {
        if (manager != null)
            manager.SetPitch(playbackId, pitch);
    }

    /// <summary>
    /// クリップ内の再生位置を0～1で直接指定する（スクラブ）。
    /// ドアの開き具合など、経過時間ではなく外部パラメータにクリップ位置を連動させたい時に使う。
    /// </summary>
    public void SetProgress(float normalized01)
    {
        if (manager != null)
            manager.SetProgress(playbackId, normalized01);
    }
}
