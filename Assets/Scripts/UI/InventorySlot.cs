//==============================================================================
//  File   : InventorySlot.cs
//  Brief  : インベントリの一個一個のスロット
// 
//  Author : Ryoto Kikuchi
//  Date   : 2026/7/7
//------------------------------------------------------------------------------
//
//==============================================================================
using System;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour {
    [SerializeField] private Image iconImage;   // アイコン表示用
    [SerializeField] private Button button;     // クリック用

    private int index;      // このスロットのインデックス
    private Action<int> onClick;    // クリック時のコールバック

    public void Init(int index, Action<int> onClick) {
        this.index = index;
        this.onClick = onClick;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => this.onClick?.Invoke(this.index));
    }

    public void SetItem(ItemBase item) {
        if (item != null && item.icon != null) {
            iconImage.sprite = item.icon;
            iconImage.enabled = true;
        } else {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
    }
}
