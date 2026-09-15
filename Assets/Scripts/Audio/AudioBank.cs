//==============================================================================
// 作成日: 2026/07/28
// 作成者: 岩崎瑛斗
// 概要: Player・Boss・UI・BGM単位で複数のサウンド設定をまとめて管理する
//==============================================================================

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プロジェクトで使用する4種類のサウンドBank。
/// </summary>
public enum AudioBankType
{
    Player,
    Boss,
    UI,
    BGM
}

/// <summary>
/// 関連する複数のサウンド設定を1つのアセットにまとめるBank。
/// </summary>
[CreateAssetMenu(fileName = "NewAudioBank", menuName = "Audio/Audio Bank")]
public sealed class AudioBank : ScriptableObject
{
    [SerializeField] private AudioBankType bankType;
    [SerializeField] private List<AudioData> entries = new List<AudioData>();

    public AudioBankType BankType => bankType;
    public IReadOnlyList<AudioData> Entries => entries;
    public AudioCategory Category =>
        bankType == AudioBankType.BGM ? AudioCategory.BGM : AudioCategory.SE;

    /// <summary>
    /// Bank名と短いIDから、ゲーム側で使用する完全な文字列IDを作る。
    /// </summary>
    public string BuildKey(string localKey)
    {
        return bankType == AudioBankType.BGM
            ? $"BGM.{localKey}"
            : $"SE.{bankType}.{localKey}";
    }

    private void OnValidate()
    {
        if (entries == null)
            return;

        // 子要素はMonoBehaviourではないため、Bank側から設定値を検証する。
        for (int i = 0; i < entries.Count; i++)
            entries[i]?.Normalize();
    }
}
