//==============================================================================
// 作成日: 2026/07/28
// 作成者: 岩崎瑛斗
// 概要: SE・BGM・AudioSourceプール・NavMesh距離減衰を一括管理する
//==============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Audio;

/// <summary>
/// Resources/Audio内のAudioBankを読み込み、SEとBGMを一元管理する。
/// シーンをまたいで利用するため、自動生成したGameObjectをDontDestroyOnLoadで保持する。
/// </summary>
[DefaultExecutionOrder(-1000)]
public sealed class AudioManager : MonoBehaviour
{
    private sealed class AudioVoice
    {
        public AudioSource Source;
        public AudioData Data;
        public Transform FollowTarget;
        public int PlaybackId;
        public float BaseVolume;
        public float FadeStartVolume;
        public float FadeDuration;
        public float FadeElapsed;
        public bool IsFadingOut;
        public readonly NavMeshPath NavMeshPath = new NavMeshPath();

        public bool IsActive => PlaybackId != 0;
    }

    private const string ResourcePath = "Audio";
    private const string MixerResourcePath = "Audio/HoraAudioMixer";
    private const int InitialSePoolSize = 16;
    private const int MaximumSePoolSize = 32;
    private const float DistanceUpdateInterval = 0.15f;

    private static AudioManager instance;

    private readonly Dictionary<string, AudioData> dataByKey =
        new Dictionary<string, AudioData>(System.StringComparer.Ordinal);
    private readonly List<AudioVoice> seVoices = new List<AudioVoice>();

    private AudioSource[] bgmSources;
    private AudioMixer audioMixer;
    private AudioMixerGroup bgmMixerGroup;
    private AudioMixerGroup seMixerGroup;
    private int activeBgmSourceIndex;
    private string currentBgmKey;
    private Coroutine bgmFadeCoroutine;
    private Transform listenerTransform;
    private int nextPlaybackId = 1;
    private float distanceUpdateTimer;
    private bool missingListenerWarningShown;

    /// <summary>
    /// 既存のAudioManagerを取得し、存在しなければ自動生成する。
    /// </summary>
    public static AudioManager GetOrCreate()
    {
        if (instance != null)
            return instance;

        instance = FindAnyObjectByType<AudioManager>();
        if (instance != null)
            return instance;

        GameObject managerObject = new GameObject("[AudioManager]");
        instance = managerObject.AddComponent<AudioManager>();
        return instance;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        GetOrCreate();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        LoadAudioData();
        LoadAudioMixer();
        CreateSePool();
        CreateBgmSources();
        ApplySavedVolume();
    }

    /// <summary>
    /// SettingsManagerで保存された音量をミキサーへ反映する。
    /// 設定画面を一度も開かなくても、前回保存された値が最初から効くようにするため
    /// （PlayerPrefsが未保存ならデフォルト値=最大音量になる）。
    /// </summary>
    private void ApplySavedVolume()
    {
        float volume = PlayerPrefs.GetFloat(SettingsManager.KEY_VOLUME, SettingsManager.DEFAULT_VOLUME);
        SetMixerVolume("BGMVolume", volume);
        SetMixerVolume("SEVolume", volume);
    }

    private void Update()
    {
        UpdateSeVoices();

        distanceUpdateTimer -= Time.unscaledDeltaTime;
        if (distanceUpdateTimer <= 0f)
        {
            distanceUpdateTimer = DistanceUpdateInterval;
            UpdateDistanceAttenuation();
        }
    }

    /// <summary>
    /// 指定したIDのSEを再生する。
    /// </summary>
    internal AudioHandle Post(
        string key,
        Transform followTarget,
        Vector3 position,
        bool hasWorldPosition)
    {
        if (!TryGetAudioData(key, AudioCategory.SE, out AudioData data))
            return null;

        AudioClip clip = data.GetNextClip();
        if (clip == null)
        {
            Debug.LogWarning($"[Audio] AudioClipが登録されていません: {key}");
            return null;
        }

        if (CountPlaying(data) >= data.MaxSimultaneous)
            return null;

        AudioVoice voice = GetAvailableVoice();
        if (voice == null)
        {
            Debug.LogWarning($"[Audio] SEプールの上限に達しました: {key}");
            return null;
        }

        int playbackId = GetNextPlaybackId();
        float baseVolume = data.GetRandomVolume();

        voice.Data = data;
        voice.FollowTarget = followTarget;
        voice.PlaybackId = playbackId;
        voice.BaseVolume = baseVolume;
        voice.FadeStartVolume = 0f;
        voice.FadeDuration = 0f;
        voice.FadeElapsed = 0f;
        voice.IsFadingOut = false;

        ConfigureSource(voice.Source, data, clip);
        voice.Source.transform.position = hasWorldPosition ? position : transform.position;
        voice.Source.volume = baseVolume;

        // 3D音は鳴り始めの一瞬だけ大音量にならないよう、再生前に減衰を反映する。
        if (data.Use3D)
            ApplyDistanceAttenuation(voice);

        voice.Source.Play();
        return new AudioHandle(this, playbackId);
    }

    /// <summary>
    /// BGMを2つのAudioSourceでクロスフェード再生する。
    /// </summary>
    internal void PlayBGM(string key, float fadeTime)
    {
        if (!TryGetAudioData(key, AudioCategory.BGM, out AudioData data))
            return;

        if (currentBgmKey == key && bgmSources[activeBgmSourceIndex].isPlaying)
            return;

        AudioClip clip = data.GetNextClip();
        if (clip == null)
        {
            Debug.LogWarning($"[Audio] AudioClipが登録されていません: {key}");
            return;
        }

        if (bgmFadeCoroutine != null)
            StopCoroutine(bgmFadeCoroutine);

        int previousIndex = activeBgmSourceIndex;
        int nextIndex = 1 - activeBgmSourceIndex;
        AudioSource previousSource = bgmSources[previousIndex];
        AudioSource nextSource = bgmSources[nextIndex];
        float targetVolume = data.GetRandomVolume();

        ConfigureSource(nextSource, data, clip);
        nextSource.loop = true;
        nextSource.volume = fadeTime > 0f ? 0f : targetVolume;
        nextSource.Play();

        activeBgmSourceIndex = nextIndex;
        currentBgmKey = key;

        if (fadeTime <= 0f)
        {
            previousSource.Stop();
            previousSource.clip = null;
            return;
        }

        bgmFadeCoroutine = StartCoroutine(
            CrossFadeBgm(previousSource, nextSource, targetVolume, fadeTime));
    }

    /// <summary>
    /// 現在のBGMを停止する。
    /// </summary>
    internal void StopBGM(float fadeTime)
    {
        if (bgmSources == null)
            return;

        if (bgmFadeCoroutine != null)
            StopCoroutine(bgmFadeCoroutine);

        currentBgmKey = null;
        bgmFadeCoroutine = StartCoroutine(FadeOutAllBgm(Mathf.Max(0f, fadeTime)));
    }

    /// <summary>
    /// 指定した再生IDが現在も有効か確認する。
    /// </summary>
    internal bool IsPlaying(int playbackId)
    {
        AudioVoice voice = FindVoice(playbackId);
        return voice != null && voice.Source.isPlaying;
    }

    /// <summary>
    /// 指定した再生IDのSEを停止する。
    /// </summary>
    internal void Stop(int playbackId, float fadeTime)
    {
        AudioVoice voice = FindVoice(playbackId);
        if (voice == null)
            return;

        if (fadeTime <= 0f)
        {
            ReleaseVoice(voice);
            return;
        }

        voice.FadeStartVolume = voice.Source.volume;
        voice.FadeDuration = fadeTime;
        voice.FadeElapsed = 0f;
        voice.IsFadingOut = true;
    }

    /// <summary>
    /// 公開されたAudioMixerパラメーターへ0～1の音量を設定する。
    /// </summary>
    internal void SetMixerVolume(string parameterName, float normalizedVolume)
    {
        if (audioMixer == null)
            return;

        float volume = Mathf.Clamp01(normalizedVolume);
        float decibel = volume <= 0.0001f
            ? -80f
            : Mathf.Log10(volume) * 20f;

        if (!audioMixer.SetFloat(parameterName, decibel))
            Debug.LogWarning($"[Audio] Mixerパラメーターが見つかりません: {parameterName}");
    }

    /// <summary>
    /// 公開されたAudioMixerパラメーターの音量を0～1へ戻して取得する。
    /// </summary>
    internal float GetMixerVolume(string parameterName)
    {
        if (audioMixer == null || !audioMixer.GetFloat(parameterName, out float decibel))
            return 1f;

        return decibel <= -80f ? 0f : Mathf.Pow(10f, decibel / 20f);
    }

    private void LoadAudioData()
    {
        dataByKey.Clear();
        AudioBank[] loadedBanks = Resources.LoadAll<AudioBank>(ResourcePath);

        foreach (AudioBank bank in loadedBanks)
        {
            if (bank == null)
                continue;

            IReadOnlyList<AudioData> entries = bank.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                AudioData data = entries[i];
                if (data == null || string.IsNullOrWhiteSpace(data.LocalKey))
                {
                    Debug.LogWarning(
                        $"[Audio] {bank.BankType} Bank内にIDが空の項目があります。",
                        bank);
                    continue;
                }

                string fullKey = bank.BuildKey(data.LocalKey);
                data.Initialize(fullKey, bank.Category);

                if (!dataByKey.TryAdd(fullKey, data))
                {
                    Debug.LogError(
                        $"[Audio] サウンドIDが重複しています: {fullKey}",
                        bank);
                }
            }
        }
    }

    private void LoadAudioMixer()
    {
        audioMixer = Resources.Load<AudioMixer>(MixerResourcePath);
        if (audioMixer == null)
        {
            Debug.LogWarning($"[Audio] AudioMixerが見つかりません: {MixerResourcePath}");
            return;
        }

        bgmMixerGroup = FindMixerGroup("BGM");
        seMixerGroup = FindMixerGroup("SE");
    }

    private AudioMixerGroup FindMixerGroup(string groupName)
    {
        AudioMixerGroup[] groups = audioMixer.FindMatchingGroups(groupName);
        if (groups.Length > 0)
            return groups[0];

        Debug.LogWarning($"[Audio] Mixer Groupが見つかりません: {groupName}", audioMixer);
        return null;
    }

    private void CreateSePool()
    {
        for (int i = 0; i < InitialSePoolSize; i++)
            seVoices.Add(CreateVoice(i));
    }

    private AudioVoice CreateVoice(int index)
    {
        GameObject sourceObject = new GameObject($"SE_{index:00}");
        sourceObject.transform.SetParent(transform, false);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;

        return new AudioVoice
        {
            Source = source
        };
    }

    private void CreateBgmSources()
    {
        bgmSources = new AudioSource[2];

        for (int i = 0; i < bgmSources.Length; i++)
        {
            GameObject sourceObject = new GameObject($"BGM_{i}");
            sourceObject.transform.SetParent(transform, false);

            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            bgmSources[i] = source;
        }
    }

    private void ConfigureSource(AudioSource source, AudioData data, AudioClip clip)
    {
        source.Stop();
        source.clip = clip;
        // 個別指定がなければ、BGMとSEのカテゴリ別Mixer Groupへ自動で送る。
        source.outputAudioMixerGroup = data.OutputMixerGroup != null
            ? data.OutputMixerGroup
            : data.Category == AudioCategory.BGM
                ? bgmMixerGroup
                : seMixerGroup;
        source.pitch = data.GetRandomPitch();
        source.loop = data.Loop;
        source.spatialBlend = data.Use3D ? 1f : 0f;
        source.dopplerLevel = 0f;

        // Unity標準の距離減衰は平坦にし、NavMesh経路距離による音量だけを使用する。
        source.rolloffMode = AudioRolloffMode.Custom;
        source.maxDistance = data.MaxDistance;
        source.SetCustomCurve(
            AudioSourceCurveType.CustomRolloff,
            AnimationCurve.Linear(0f, 1f, data.MaxDistance, 1f));
    }

    private void UpdateSeVoices()
    {
        for (int i = 0; i < seVoices.Count; i++)
        {
            AudioVoice voice = seVoices[i];
            if (!voice.IsActive)
                continue;

            if (voice.FollowTarget != null)
                voice.Source.transform.position = voice.FollowTarget.position;

            if (voice.IsFadingOut)
            {
                voice.FadeElapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(voice.FadeElapsed / voice.FadeDuration);
                voice.Source.volume = Mathf.Lerp(voice.FadeStartVolume, 0f, progress);

                if (progress >= 1f)
                {
                    ReleaseVoice(voice);
                    continue;
                }
            }

            if (!voice.Source.isPlaying)
                ReleaseVoice(voice);
        }
    }

    private void UpdateDistanceAttenuation()
    {
        ResolveListener();
        if (listenerTransform == null)
            return;

        for (int i = 0; i < seVoices.Count; i++)
        {
            AudioVoice voice = seVoices[i];
            if (voice.IsActive && voice.Data.Use3D && !voice.IsFadingOut)
                ApplyDistanceAttenuation(voice);
        }
    }

    private void ApplyDistanceAttenuation(AudioVoice voice)
    {
        ResolveListener();
        if (listenerTransform == null)
            return;

        Vector3 sourcePosition = voice.Source.transform.position;
        Vector3 listenerPosition = listenerTransform.position;
        float distance = CalculateAudibleDistance(
            sourcePosition,
            listenerPosition,
            voice.Data.NavMeshSampleRadius,
            voice.NavMeshPath);

        voice.Source.volume = voice.BaseVolume * voice.Data.EvaluateAttenuation(distance);
    }

    /// <summary>
    /// 音源とリスナーに最も近いNavMesh点の経路距離を取得する。
    /// NavMesh点または完全な経路を取得できない場合は直線距離へフォールバックする。
    /// </summary>
    private static float CalculateAudibleDistance(
        Vector3 sourcePosition,
        Vector3 listenerPosition,
        float sampleRadius,
        NavMeshPath path)
    {
        bool sourceFound = NavMesh.SamplePosition(
            sourcePosition,
            out NavMeshHit sourceHit,
            sampleRadius,
            NavMesh.AllAreas);
        bool listenerFound = NavMesh.SamplePosition(
            listenerPosition,
            out NavMeshHit listenerHit,
            sampleRadius,
            NavMesh.AllAreas);

        if (!sourceFound || !listenerFound)
            return Vector3.Distance(sourcePosition, listenerPosition);

        bool pathFound = NavMesh.CalculatePath(
            sourceHit.position,
            listenerHit.position,
            NavMesh.AllAreas,
            path);

        if (!pathFound || path.status != NavMeshPathStatus.PathComplete)
        {
            return Vector3.Distance(sourcePosition, listenerPosition);
        }

        // 同じNavMeshポリゴン内など、曲がり角がない場合はNavMesh点同士の直線距離を使う。
        if (path.corners.Length < 2)
            return Vector3.Distance(sourceHit.position, listenerHit.position);

        float pathDistance = 0f;
        for (int i = 0; i < path.corners.Length - 1; i++)
            pathDistance += Vector3.Distance(path.corners[i], path.corners[i + 1]);

        return pathDistance;
    }

    private void ResolveListener()
    {
        if (listenerTransform != null && listenerTransform.gameObject.activeInHierarchy)
            return;

        AudioListener listener = FindAnyObjectByType<AudioListener>();
        listenerTransform = listener != null
            ? listener.transform
            : Camera.main != null
                ? Camera.main.transform
                : null;

        if (listenerTransform == null && !missingListenerWarningShown)
        {
            missingListenerWarningShown = true;
            Debug.LogWarning("[Audio] AudioListenerまたはMainCameraが見つかりません。");
        }
    }

    private bool TryGetAudioData(
        string key,
        AudioCategory expectedCategory,
        out AudioData data)
    {
        data = null;

        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.LogWarning("[Audio] 空のIDは再生できません。");
            return false;
        }

        if (!dataByKey.TryGetValue(key, out data))
        {
            Debug.LogWarning(
                $"[Audio] サウンドが見つかりません: {key} " +
                $"(Player / Boss / UI / BGM Bankを確認してください)");
            return false;
        }

        if (data.Category != expectedCategory)
        {
            Debug.LogWarning(
                $"[Audio] カテゴリが一致しません: {key} " +
                $"(設定: {data.Category}, 呼び出し: {expectedCategory})");
            return false;
        }

        return true;
    }

    private AudioVoice GetAvailableVoice()
    {
        for (int i = 0; i < seVoices.Count; i++)
        {
            if (!seVoices[i].IsActive)
                return seVoices[i];
        }

        if (seVoices.Count >= MaximumSePoolSize)
            return null;

        AudioVoice voice = CreateVoice(seVoices.Count);
        seVoices.Add(voice);
        return voice;
    }

    private int CountPlaying(AudioData data)
    {
        int count = 0;

        for (int i = 0; i < seVoices.Count; i++)
        {
            if (seVoices[i].IsActive && seVoices[i].Data == data)
                count++;
        }

        return count;
    }

    private AudioVoice FindVoice(int playbackId)
    {
        if (playbackId == 0)
            return null;

        for (int i = 0; i < seVoices.Count; i++)
        {
            if (seVoices[i].PlaybackId == playbackId)
                return seVoices[i];
        }

        return null;
    }

    private int GetNextPlaybackId()
    {
        if (nextPlaybackId == int.MaxValue)
            nextPlaybackId = 1;

        return nextPlaybackId++;
    }

    private void ReleaseVoice(AudioVoice voice)
    {
        voice.Source.Stop();
        voice.Source.clip = null;
        voice.Source.outputAudioMixerGroup = null;
        voice.Source.volume = 0f;
        voice.Data = null;
        voice.FollowTarget = null;
        voice.PlaybackId = 0;
        voice.IsFadingOut = false;
    }

    private IEnumerator CrossFadeBgm(
        AudioSource previousSource,
        AudioSource nextSource,
        float targetVolume,
        float duration)
    {
        float previousStartVolume = previousSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            previousSource.volume = Mathf.Lerp(previousStartVolume, 0f, progress);
            nextSource.volume = Mathf.Lerp(0f, targetVolume, progress);
            yield return null;
        }

        previousSource.Stop();
        previousSource.clip = null;
        nextSource.volume = targetVolume;
        bgmFadeCoroutine = null;
    }

    private IEnumerator FadeOutAllBgm(float duration)
    {
        float[] startVolumes =
        {
            bgmSources[0].volume,
            bgmSources[1].volume
        };

        if (duration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);

                for (int i = 0; i < bgmSources.Length; i++)
                    bgmSources[i].volume = Mathf.Lerp(startVolumes[i], 0f, progress);

                yield return null;
            }
        }

        for (int i = 0; i < bgmSources.Length; i++)
        {
            bgmSources[i].Stop();
            bgmSources[i].clip = null;
            bgmSources[i].volume = 0f;
        }

        bgmFadeCoroutine = null;
    }
}
