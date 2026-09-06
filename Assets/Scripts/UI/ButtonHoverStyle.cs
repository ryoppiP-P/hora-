using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class ButtonHoverStyle : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler {

    [SerializeField] private Image background;
    [SerializeField] private TMP_Text label;

    [Header("Normal")]
    [SerializeField] private Color normalBg = new Color(1, 1, 1, 0);     // ����
    [SerializeField] private Color normalText = Color.white;             // ������

    [Header("Highlighted")]
    [SerializeField] private Color hoverBg = Color.white;                // ���w�i
    [SerializeField] private Color hoverText = Color.black;              // ������

    private bool isHighlighted;

    void Awake() {
        Apply(false);
    }

    void Apply(bool hovered) {
        if (background != null) background.color = hovered ? hoverBg : normalBg;
        if (label != null) label.color = hovered ? hoverText : normalText;

        // OnPointerEnterとOnSelectが同時に来た時に2回鳴らさないよう、
        // 実際に非選択→選択へ切り替わった瞬間だけ再生する
        if (hovered && !isHighlighted) {
            Audio.Post("SE.UI.Select");
        }
        isHighlighted = hovered;
    }

    public void OnPointerEnter(PointerEventData e) => Apply(true);
    public void OnPointerExit(PointerEventData e) => Apply(false);
    public void OnSelect(BaseEventData e) => Apply(true);
    public void OnDeselect(BaseEventData e) => Apply(false);
}
