//==============================================================================
//  File   : Inventory.cs
//  Brief  : インベントリ本体
// 
//  Author : Ryoto Kikuchi
//  Date   : 2026/7/7
//------------------------------------------------------------------------------
//
//==============================================================================
using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour {
    [SerializeField] private int capacity = 9;      // インベントリの最大スロット数
    private readonly List<ItemBase> slots = new List<ItemBase>();   // スロットの中身 nullなら空きスロット

    public event Action OnChanged;      // インベントリの中身が変化した時に呼ばれるイベント
    public int Capacity => capacity;    // 最大スロット数ゲッター
    public IReadOnlyList<ItemBase> Slots => slots;  // スロットの中身ゲッター

    void Awake() {
        for (int i = 0; i < capacity; i++) slots.Add(null);
    }

    public bool IsFull {
        get {
            foreach (var s in slots) if (s == null) return false;
            return true;
        }
    }

    // 空きスロットに追加。成功で true
    public bool TryAdd(ItemBase item) {
        for (int i = 0; i < slots.Count; i++) {
            if (slots[i] == null) {
                slots[i] = item;
                OnChanged?.Invoke();
                return true;
            }
        }
        return false;
    }

    // 指定index を取り出して返す（無ければ null）
    public ItemBase TakeAt(int index) {
        if (index < 0 || index >= slots.Count) return null;
        var it = slots[index];
        if (it == null) return null;
        slots[index] = null;
        OnChanged?.Invoke();
        return it;
    }

    // 特定アイテムを1個消費（同じアイテムが複数あっても1個だけ）
    public bool RemoveOne(ItemBase item) {
        for (int i = 0; i < slots.Count; i++) {
            if (slots[i] == item) {
                slots[i] = null;
                OnChanged?.Invoke();
                return true;
            }
        }
        return false;
    }
}
