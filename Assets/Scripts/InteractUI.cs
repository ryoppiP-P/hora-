using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractUI : MonoBehaviour {
    [SerializeField] private GameObject root;       // ƒpƒlƒ‹‘S‘Ì
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private Image holdGauge;       // Filled Image

    public void UpdateUI(Interactable target, float held, float duration) {
        if (root == null) return;

        if (target == null) {
            root.SetActive(false);
            return;
        }

        root.SetActive(true);
        if (promptText != null) promptText.text = target.PromptText;
        if (holdGauge != null)
            holdGauge.fillAmount = (duration <= 0f) ? 0f : Mathf.Clamp01(held / duration);
    }
}
