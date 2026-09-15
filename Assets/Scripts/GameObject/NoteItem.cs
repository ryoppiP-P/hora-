using UnityEngine;

[CreateAssetMenu(fileName = "NewNote", menuName = "Inventory/Note Item")]
public class NoteItem : ItemBase {
    [TextArea(3, 8)]
    public string noteText = "";
}
