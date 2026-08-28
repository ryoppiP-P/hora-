using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// タイトル→ゲーム遷移の時だけ流すオープニング動画。
/// StreamingAssets/opVideo.mp4 をURLで再生する（WebGLはVideoClipの直参照が使えず、
/// StreamingAssets経由のURL再生でないと動かないため）。
/// </summary>
public static class OpeningMovie {
    /// <summary>
    /// TitleManagerのStartボタンから遷移する直前にtrueにする。
    /// 再生開始時に即falseへ戻すので、死亡後のリスタート（同シーン再ロード）や
    /// エディタでゲームシーンから直接再生した場合は流れない。
    /// </summary>
    public static bool ShouldPlay = false;

    private const string FileName = "opVideo.mp4";
    private const int CanvasSortingOrder = 200; // FadeManagerのCanvasFade(sortingOrder=100)より手前
    private const string FontResourcePath = "04HomuraM-Medium SDF";
    private const float SkipHoldDuration = 1.0f; // 秒。この時間SPACEを押し続けたらスキップ
    private const float FadeDuration = 1.5f; // 再生前後のフェード時間（秒）

    /// <summary>
    /// ShouldPlayがtrueの時だけ動画を最後まで（またはSPACE長押しスキップで）再生する。
    /// falseなら即座に完了するのでIntroCutscene側からは常にyield returnして安全。
    /// </summary>
    public static IEnumerator PlayIfNeeded() {
        if (!ShouldPlay) yield break;
        ShouldPlay = false;

        string path = Path.Combine(Application.streamingAssetsPath, FileName);

        GameObject canvasObj = new GameObject("OpeningMovieCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CanvasSortingOrder;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // 背景は常に黒（動画の準備が終わるまでの間や、動画の外側の余白、
        // フェードイン/アウトの間に見える下地）。これ自体はフェードさせない
        GameObject bgObj = new GameObject("Black");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bg = bgObj.AddComponent<Image>();
        bg.color = Color.black;
        RectTransform bgRect = bg.rectTransform;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // 動画本体とスキップUIをまとめてフェードさせるためのグループ
        GameObject fadeGroupObj = new GameObject("FadeGroup");
        fadeGroupObj.transform.SetParent(canvasObj.transform, false);
        RectTransform fadeGroupRect = fadeGroupObj.AddComponent<RectTransform>();
        fadeGroupRect.anchorMin = Vector2.zero;
        fadeGroupRect.anchorMax = Vector2.one;
        fadeGroupRect.offsetMin = Vector2.zero;
        fadeGroupRect.offsetMax = Vector2.zero;
        CanvasGroup fadeGroup = fadeGroupObj.AddComponent<CanvasGroup>();
        fadeGroup.alpha = 0f;

        GameObject videoObj = new GameObject("VideoImage");
        videoObj.transform.SetParent(fadeGroupObj.transform, false);
        RawImage rawImage = videoObj.AddComponent<RawImage>();
        rawImage.color = Color.white;
        RectTransform videoRect = rawImage.rectTransform;
        videoRect.anchorMin = Vector2.zero;
        videoRect.anchorMax = Vector2.one;
        videoRect.offsetMin = Vector2.zero;
        videoRect.offsetMax = Vector2.zero;

        RenderTexture renderTexture = new RenderTexture(1920, 1080, 0);
        rawImage.texture = renderTexture;

        Image fillImage;
        BuildSkipPrompt(fadeGroupObj.transform, out fillImage);

        VideoPlayer videoPlayer = canvasObj.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = path;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;

        AudioSource audioSource = canvasObj.AddComponent<AudioSource>();
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.SetTargetAudioSource(0, audioSource);

        bool prepared = false;
        videoPlayer.prepareCompleted += _ => prepared = true;
        bool errored = false;
        videoPlayer.errorReceived += (_, msg) => {
            Debug.LogWarning($"[OpeningMovie] 再生に失敗しました: {msg}");
            errored = true;
        };
        videoPlayer.Prepare();

        float prepareTimeout = 10f;
        while (!prepared && !errored && prepareTimeout > 0f) {
            prepareTimeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (prepared && !errored) {
            videoPlayer.Play();

            yield return FadeCanvasGroup(fadeGroup, 0f, 1f, FadeDuration);

            bool finished = false;
            videoPlayer.loopPointReached += _ => finished = true;

            float skipHoldTimer = 0f;
            while (!finished && !errored) {
                Keyboard kb = Keyboard.current;
                bool holding = kb != null && kb.spaceKey.isPressed;
                skipHoldTimer = holding ? skipHoldTimer + Time.unscaledDeltaTime : 0f;

                if (fillImage != null) fillImage.fillAmount = skipHoldTimer / SkipHoldDuration;

                if (skipHoldTimer >= SkipHoldDuration) break;
                yield return null;
            }

            yield return FadeCanvasGroup(fadeGroup, fadeGroup.alpha, 0f, FadeDuration);

            videoPlayer.Stop();
        }

        Object.Destroy(renderTexture);
        Object.Destroy(canvasObj);
    }

    private static IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration) {
        float t = 0f;
        while (t < duration) {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        group.alpha = to;
    }

    /// <summary>右下にSPACE長押しスキップのプロンプト（ラベル+進捗バー）を作る</summary>
    private static void BuildSkipPrompt(Transform parent, out Image fillImage) {
        GameObject root = new GameObject("SkipPrompt");
        root.transform.SetParent(parent, false);
        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(1f, 0f);
        rootRect.anchorMax = new Vector2(1f, 0f);
        rootRect.pivot = new Vector2(1f, 0f);
        rootRect.anchoredPosition = new Vector2(-40f, 40f);
        rootRect.sizeDelta = new Vector2(280f, 60f);

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>(FontResourcePath);

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(root.transform, false);
        TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = "SPACE長押しでスキップ";
        label.fontSize = 24f;
        label.alignment = TextAlignmentOptions.Right;
        label.color = Color.white;
        if (font != null) label.font = font;
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(1f, 1f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(0f, 30f);

        Sprite whiteSprite = CreateWhiteSprite();

        GameObject barBgObj = new GameObject("BarBackground");
        barBgObj.transform.SetParent(root.transform, false);
        Image barBg = barBgObj.AddComponent<Image>();
        barBg.sprite = whiteSprite;
        barBg.color = new Color(1f, 1f, 1f, 0.25f);
        RectTransform barBgRect = barBg.rectTransform;
        barBgRect.anchorMin = new Vector2(0f, 0f);
        barBgRect.anchorMax = new Vector2(1f, 0f);
        barBgRect.pivot = new Vector2(1f, 0f);
        barBgRect.anchoredPosition = Vector2.zero;
        barBgRect.sizeDelta = new Vector2(0f, 8f);

        GameObject barFillObj = new GameObject("BarFill");
        barFillObj.transform.SetParent(root.transform, false);
        Image barFill = barFillObj.AddComponent<Image>();
        // Image.Type.Filledはsprite未設定だとfillAmountを変えても見た目に反映されないため、
        // 実行時生成の白スプライトを明示的に割り当てる
        barFill.sprite = whiteSprite;
        barFill.color = Color.white;
        barFill.type = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;
        barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        barFill.fillAmount = 0f;
        RectTransform barFillRect = barFill.rectTransform;
        barFillRect.anchorMin = new Vector2(0f, 0f);
        barFillRect.anchorMax = new Vector2(1f, 0f);
        barFillRect.pivot = new Vector2(1f, 0f);
        barFillRect.anchoredPosition = Vector2.zero;
        barFillRect.sizeDelta = new Vector2(0f, 8f);

        fillImage = barFill;
    }

    private static Sprite CreateWhiteSprite() {
        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[4 * 4];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
    }
}
