//==============================================================================
//  File   : ItemPickup.cs
//  Brief  : アイテムを拾うためのインタラクタブル
// 
//  Author : Ryoto Kikuchi
//  Date   : 2026/7/7
//------------------------------------------------------------------------------
//
//==============================================================================
using UnityEngine;

public class ItemPickup : Interactable {
    [SerializeField] private ItemBase item;     // 拾えるアイテム

    public override string PromptText => item != null ? $"{item.itemName} を拾う" : "拾う";  // インタラクトUIに表示するテキスト

    public override void OnInteractComplete(Player player) {
        if (item == null) return;
        var inv = player.GetComponent<Inventory>();
        if (inv == null) { Debug.LogWarning("Player に Inventory が無い"); return; }

        if (inv.IsFull) {
            Debug.Log("インベントリが満杯");
            return;
        }

        // ノートは先に読ませてから閉じたタイミングでインベントリへ追加する
        if (item is NoteItem note) {
            NoteReaderUI.Show(note, player, () => {
                if (inv.TryAdd(item)) Audio.Post("SE.Player.Item.Pickup", transform.position);
                Destroy(gameObject);
            });
            return;
        }

        if (inv.TryAdd(item)) {
            Audio.Post("SE.Player.Item.Pickup", transform.position);
            Destroy(gameObject);
        } else {
            Debug.Log("インベントリが満杯");
        }
    }
}
