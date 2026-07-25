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
    [SerializeField] private Color normalBg = new Color(1, 1, 1, 0);     // “§–¾
    [SerializeField] private Color normalText = Color.white;             // ”’•¶Žš

    [Header("Highlighted")]
    [SerializeField] private Color hoverBg = Color.white;                // ”’”wŒi
    [SerializeField] private Color hoverText = Color.black;              // ••¶Žš

    void Awake() {
        Apply(false);
    }

    void Apply(bool hovered) {
        if (background != null) background.color = hovered ? hoverBg : normalBg;
        if (label != null) label.color = hovered ? hoverText : normalText;
    }

    public void OnPointerEnter(PointerEventData e) => Apply(true);
    public void OnPointerExit(PointerEventData e) => Apply(false);
    public void OnSelect(BaseEventData e) => Apply(true);
    public void OnDeselect(BaseEventData e) => Apply(false);
}
