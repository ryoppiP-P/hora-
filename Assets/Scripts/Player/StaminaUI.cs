using UnityEngine;
using UnityEngine.UI;

public class StaminaUI : MonoBehaviour {
    [SerializeField] private Player player;
    [SerializeField] private Image fillImage;       // バー本体
    [SerializeField] private CanvasGroup canvasGroup; // フェード用（任意）

    [Header("Colors")]
    [SerializeField] private Color fullColor = new Color(0.4f, 0.9f, 0.4f, 1f);
    [SerializeField] private Color lowColor = new Color(0.9f, 0.3f, 0.3f, 1f);
    [SerializeField] private float lowThreshold = 0.3f;

    [Header("Auto Hide")]
    [SerializeField] private bool autoHideWhenFull = true;
    [SerializeField] private float fadeSpeed = 4f;

    void Update() {
        if (player == null || fillImage == null) return;

        float ratio = player.StaminaRatio;

        // バーの長さ
        fillImage.fillAmount = ratio;

        // 色（しきい値以下で赤に近づく）
        fillImage.color = Color.Lerp(lowColor, fullColor, Mathf.InverseLerp(0f, lowThreshold, ratio));
        // しきい値超えたら fullColor 固定にしたい場合:
        if (ratio > lowThreshold) fillImage.color = fullColor;

        // 満タン時に自動で隠す
        if (canvasGroup != null && autoHideWhenFull) {
            float targetAlpha = (ratio >= 0.999f) ? 0f : 1f;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
        }
    }
}
