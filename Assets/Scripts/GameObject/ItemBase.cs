//==============================================================================
//  File   : ItemBase.cs
//  Brief  : インベントリのアイテム基底クラス
// 
//  Author : Ryoto Kikuchi
//  Date   : 2026/7/7
//------------------------------------------------------------------------------
//
//==============================================================================
using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class ItemBase : ScriptableObject {
    public string itemName = "Item";    // アイテム名
    public Sprite icon;                 // アイコン画像
    public GameObject worldPrefab;      // 落とす時に生成するプレハブ（ItemPickupが付いたもの）
}
