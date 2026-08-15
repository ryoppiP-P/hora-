using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>拾ったノートなどのテキストアイテムを、写真を見るような演出で全画面表示する</summary>
public class NoteReaderUI : MonoBehaviour {
    public static bool IsOpen { get; private set; }

    private const float FadeDuration = 0.35f;
    private const float InputGuardDuration = 0.2f; // 開いた直後の入力で即座に閉じてしまうのを防ぐ

    private static NoteReaderUI instance;
    private static GameObject canvasObject;
    private static CanvasGroup canvasGroup;
    private static RectTransform paperRect;
    private static TMP_Text noteText;

    private Player currentPlayer;
    private System.Action onClosed;
    private float animT;
    private float inputGuardTimer;

    static void Init() {
        canvasObject = new GameObject("NoteReaderCanvas");
        DontDestroyOnLoad(canvasObject);

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        canvasObject.AddComponent<GraphicRaycaster>();

        canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        instance = canvasObject.AddComponent<NoteReaderUI>();

        // 背景の暗幕（家族写真を見る時のように画面を暗くして注目させる）
        var dimGO = new GameObject("Dim");
        dimGO.transform.SetParent(canvasObject.transform, false);
        var dimImage = dimGO.AddComponent<Image>();
        dimImage.color = new Color(0f, 0f, 0f, 0.75f);
        dimImage.raycastTarget = false;
        var dimRT = dimImage.rectTransform;
        dimRT.anchorMin = Vector2.zero;
        dimRT.anchorMax = Vector2.one;
        dimRT.offsetMin = Vector2.zero;
        dimRT.offsetMax = Vector2.zero;

        // 紙のImage（Resources/paper.png）
        var paperGO = new GameObject("Paper");
        paperGO.transform.SetParent(canvasObject.transform, false);
        var paperImage = paperGO.AddComponent<Image>();
        paperImage.raycastTarget = false;
        paperImage.preserveAspect = true;
        paperImage.sprite = Resources.Load<Sprite>("paper");

        paperRect = paperImage.rectTransform;
        paperRect.anchorMin = new Vector2(0.5f, 0.5f);
        paperRect.anchorMax = new Vector2(0.5f, 0.5f);
        paperRect.pivot = new Vector2(0.5f, 0.5f);
        paperRect.anchoredPosition = Vector2.zero;

        float aspect = 0.75f;
        if (paperImage.sprite != null)
            aspect = paperImage.sprite.rect.width / paperImage.sprite.rect.height;
        const float targetHeight = 820f;
        paperRect.sizeDelta = new Vector2(targetHeight * aspect, targetHeight);

        // テキスト（紙の破れていない上半分に収める）
        var textGO = new GameObject("NoteText");
        textGO.transform.SetParent(paperGO.transform, false);
        noteText = textGO.AddComponent<TextMeshProUGUI>();
        noteText.font = Resources.Load<TMP_FontAsset>("04HomuraM-Medium SDF");
        noteText.alignment = TextAlignmentOptions.Center;
        noteText.color = new Color(0.16f, 0.11f, 0.07f);
        noteText.fontSize = 40f;

        var textRT = noteText.rectTransform;
        textRT.anchorMin = new Vector2(0.15f, 0.55f);
        textRT.anchorMax = new Vector2(0.85f, 0.86f);
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        canvasObject.SetActive(true);
    }

    /// <summary>ノートを画面いっぱいに表示する。閉じられたらonClosedが呼ばれる</summary>
    public static void Show(NoteItem note, Player player, System.Action onClosed) {
        if (note == null || player == null) return;
        if (canvasObject == null) Init();

        noteText.text = note.noteText;

        instance.currentPlayer = player;
        instance.onClosed = onClosed;
        instance.animT = 0f;
        instance.inputGuardTimer = InputGuardDuration;
        paperRect.localScale = Vector3.one * 0.85f;
        canvasGroup.alpha = 0f;

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        player.SetInputLocked(true);

        canvasGroup.blocksRaycasts = true;
        IsOpen = true;
    }

    void Update() {
        if (!IsOpen) return;

        animT = Mathf.Min(1f, animT + Time.unscaledDeltaTime / FadeDuration);
        float eased = 1f - (1f - animT) * (1f - animT);
        canvasGroup.alpha = eased;
        float scale = Mathf.Lerp(0.85f, 1f, eased);
        paperRect.localScale = new Vector3(scale, scale, 1f);

        if (inputGuardTimer > 0f) {
            inputGuardTimer -= Time.unscaledDeltaTime;
            return;
        }

        var kb = Keyboard.current;
        var mouse = Mouse.current;
        bool closePressed =
            (kb != null && kb.anyKey.wasPressedThisFrame) ||
            (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame));

        if (closePressed) Close();
    }

    void Close() {
        IsOpen = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0f;

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (currentPlayer != null) currentPlayer.SetInputLocked(false);

        var cb = onClosed;
        currentPlayer = null;
        onClosed = null;
        cb?.Invoke();
    }
}
