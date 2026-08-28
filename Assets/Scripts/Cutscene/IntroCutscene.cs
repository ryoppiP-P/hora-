using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class IntroCutscene : MonoBehaviour {
    [Header("References")]
    public Player player;
    public CameraLook cameraLook;
    public Image blinkOverlay;       // 画面全体を覆う黒Image（まばたき用）
    public CanvasGroup photoUI;      // 家族写真UI（CanvasGroupを付ける）

    [Header("Photo Item")]
    public PhotoItem familyPhoto;       // 目覚めた瞬間にインベントリへ入れる写真アイテム
    public GameObject photoFrameObject; // 写真UI表示後に消す床の写真立て

    [Header("Camera Angles")]
    public float ceilingPitch = -70f;   // 天井を見上げる
    public float photoPitch = 55f;   // 右斜め下
    public float photoYaw = 25f;   // 右向き
    public float floorPitch = 75f;   // ほぼ真下（浸水確認）
    public float standPitch = 0f;   // 立ち上がった後は正面

    [Header("Timing")]
    public float blinkDuration = 0.25f;
    public int blinkCount = 3;
    public float lookPhotoTime = 1.5f;
    public float photoShowTime = 3.0f;
    public float lookFloorTime = 1.5f;
    public float standUpTime = 1.2f;

    void Start() {
        StartCoroutine(Play());
    }

    IEnumerator Play() {
        // 入力ロック
        if (player != null) player.SetInputLocked(true);
        if (cameraLook != null) cameraLook.cutsceneControlled = true;

        yield return OpeningMovie.PlayIfNeeded();

        // 初期姿勢：天井を見ている
        cameraLook.SetRotation(ceilingPitch, 0f);
        if (photoUI != null) { photoUI.alpha = 0f; photoUI.gameObject.SetActive(false); }
        if (blinkOverlay != null) SetOverlayAlpha(1f); // 最初は真っ黒

        // BlinkOverlayを有効化して真っ黒からスタート
        if (blinkOverlay != null) {
            blinkOverlay.gameObject.SetActive(true);
            SetOverlayAlpha(1f);
        }

        // ① フェードイン（目が覚める）
        yield return Fade(1f, 0f, 1.0f);

        // ② まばたき
        for (int i = 0; i < blinkCount; i++) {
            yield return Fade(0f, 1f, blinkDuration * 0.4f);
            yield return new WaitForSeconds(blinkDuration * 0.2f);
            yield return Fade(1f, 0f, blinkDuration * 0.4f);
            yield return new WaitForSeconds(0.3f);
        }

        yield return new WaitForSeconds(0.6f);

        // ③ 家族写真の方向を向く（滑らかに）
        yield return RotateCamera(ceilingPitch, 0f, photoPitch, photoYaw, lookPhotoTime);

        yield return new WaitForSeconds(0.9f);

        // ④ 写真を拾う（インベントリへ追加）。見た目はこの後モデルをクローズアップ撮影するので、
        // 写真立て自体はまだ消さない
        if (player != null && familyPhoto != null) {
            var inv = player.GetComponent<Inventory>();
            if (inv != null) inv.TryAdd(familyPhoto);
        }

        // ⑤ 写真立て（額縁ごと）を焼き込み済みの静止画で全画面フェード表示
        // ※ライブカメラ+ライトでのクローズアップ撮影はやめた（プレイヤーのカメラにライトが映り込んでしまうため）。
        //   代わりに事前にレンダリングしたAssets/Resources/FamilyPhotoFramed.pngをそのまま表示する
        if (photoUI != null) {
            photoUI.gameObject.SetActive(true);
            yield return FadeCanvas(photoUI, 0f, 1f, 0.5f);
            yield return new WaitForSeconds(photoShowTime);
            yield return FadeCanvas(photoUI, 1f, 0f, 0.5f);
            photoUI.gameObject.SetActive(false);
        }

        if (photoFrameObject != null) Destroy(photoFrameObject);

        // ⑥ 床を見渡す（浸水を確認）
        yield return RotateCamera(photoPitch, photoYaw, floorPitch, 0f, lookFloorTime);
        yield return new WaitForSeconds(1.2f); // 浸水の絶望を噛みしめる間

        // ⑦ 立ち上がる（正面へ）
        yield return RotateCamera(floorPitch, 0f, standPitch, 0f, standUpTime);

        // 操作復帰
        if (cameraLook != null) cameraLook.cutsceneControlled = false;
        if (player != null) player.SetInputLocked(false);
    }

    IEnumerator RotateCamera(float p0, float y0, float p1, float y1, float duration) {
        float t = 0f;
        while (t < duration) {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            cameraLook.SetRotation(Mathf.Lerp(p0, p1, k), Mathf.Lerp(y0, y1, k));
            yield return null;
        }
        cameraLook.SetRotation(p1, y1);
    }

    IEnumerator Fade(float from, float to, float duration) {
        if (blinkOverlay == null) yield break;
        float t = 0f;
        while (t < duration) {
            t += Time.deltaTime;
            SetOverlayAlpha(Mathf.Lerp(from, to, t / duration));
            yield return null;
        }
        SetOverlayAlpha(to);
    }

    IEnumerator FadeCanvas(CanvasGroup cg, float from, float to, float duration) {
        float t = 0f;
        while (t < duration) {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        cg.alpha = to;
    }

    void SetOverlayAlpha(float a) {
        var c = blinkOverlay.color;
        c.a = a;
        blinkOverlay.color = c;
    }

}
