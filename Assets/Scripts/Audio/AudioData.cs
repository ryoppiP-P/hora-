//==============================================================================
// 作成日: 2026/07/28
// 作成者: 岩崎瑛斗
// 概要: AudioBank内で1つのサウンドに必要な再生設定を管理するデータ
//==============================================================================

using System;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// サウンドの用途を表すカテゴリ。
/// </summary>
public enum AudioCategory
{
    SE,
    BGM
}

/// <summary>
/// 複数の波形から再生する音を選ぶ方法。
/// </summary>
public enum AudioClipSelection
{
    Random,
    RoundRobin
}

/// <summary>
/// AudioBank内の1項目として保存されるサウンド設定。
/// </summary>
[Serializable]
public sealed class AudioData
{
    [Header("基本設定")]
    [SerializeField] private string key;
    [SerializeField] private AudioClip[] clips = Array.Empty<AudioClip>();
    [SerializeField] private AudioClipSelection clipSelection = AudioClipSelection.Random;
    [SerializeField] private AudioMixerGroup outputMixerGroup;

    [Header("再生設定")]
    [SerializeField] private Vector2 volumeRange = Vector2.one;
    [SerializeField] private Vector2 pitchRange = Vector2.one;
    [SerializeField] private bool loop;
    [Min(1)]
    [SerializeField] private int maxSimultaneous = 4;

    [Header("3D・距離減衰")]
    [SerializeField] private bool use3D;
    [Min(0.01f)]
    [SerializeField] private float maxDistance = 25f;
    [Min(0.01f)]
    [SerializeField] private float navMeshSampleRadius = 3f;
    [SerializeField] private AnimationCurve attenuationCurve =
        AnimationCurve.Linear(0f, 1f, 1f, 0f);

    [NonSerialized] private int roundRobinIndex;
    [NonSerialized] private string runtimeKey;
    [NonSerialized] private AudioCategory runtimeCategory;

    /// <summary>
    /// Bank内で使用する短いID。例: Footstep、Attack、Select。
    /// </summary>
    public string LocalKey => key;

    /// <summary>
    /// Bank名を含む実行時ID。例: SE.Player.Footstep。
    /// </summary>
    public string Key => runtimeKey;
    public AudioCategory Category => runtimeCategory;
    public AudioMixerGroup OutputMixerGroup => outputMixerGroup;
    public bool Loop => loop;
    public bool Use3D => use3D && runtimeCategory == AudioCategory.SE;
    public int MaxSimultaneous => Mathf.Max(1, maxSimultaneous);
    public float MaxDistance => Mathf.Max(0.01f, maxDistance);
    public float NavMeshSampleRadius => Mathf.Max(0.01f, navMeshSampleRadius);

    /// <summary>
    /// 設定された方法に従い、今回再生するAudioClipを取得する。
    /// </summary>
    public AudioClip GetNextClip()
    {
        if (clips == null || clips.Length == 0)
            return null;

        if (clipSelection == AudioClipSelection.Random)
            return clips[UnityEngine.Random.Range(0, clips.Length)];

        AudioClip clip = clips[roundRobinIndex % clips.Length];
        roundRobinIndex = (roundRobinIndex + 1) % clips.Length;
        return clip;
    }

    /// <summary>
    /// 設定範囲内から今回の再生音量を決める。
    /// </summary>
    public float GetRandomVolume()
    {
        return UnityEngine.Random.Range(volumeRange.x, volumeRange.y);
    }

    /// <summary>
    /// 設定範囲内から今回の再生ピッチを決める。
    /// </summary>
    public float GetRandomPitch()
    {
        return UnityEngine.Random.Range(pitchRange.x, pitchRange.y);
    }

    /// <summary>
    /// 正規化した距離から最終的な減衰率を取得する。
    /// </summary>
    public float EvaluateAttenuation(float distance)
    {
        float normalizedDistance = Mathf.Clamp01(distance / MaxDistance);
        return Mathf.Clamp01(attenuationCurve.Evaluate(normalizedDistance));
    }

    /// <summary>
    /// AudioBankから読み込まれたときに完全なIDとカテゴリを設定する。
    /// </summary>
    internal void Initialize(string fullKey, AudioCategory category)
    {
        runtimeKey = fullKey;
        runtimeCategory = category;
        Normalize();
    }

    /// <summary>
    /// Inspectorで範囲を逆に入力しても安全に利用できるよう設定値を整える。
    /// </summary>
    internal void Normalize()
    {
        volumeRange.x = Mathf.Max(0f, volumeRange.x);
        volumeRange.y = Mathf.Max(volumeRange.x, volumeRange.y);
        pitchRange.x = Mathf.Max(0.01f, pitchRange.x);
        pitchRange.y = Mathf.Max(pitchRange.x, pitchRange.y);
        maxSimultaneous = Mathf.Max(1, maxSimultaneous);
        maxDistance = Mathf.Max(0.01f, maxDistance);
        navMeshSampleRadius = Mathf.Max(0.01f, navMeshSampleRadius);
    }
}
