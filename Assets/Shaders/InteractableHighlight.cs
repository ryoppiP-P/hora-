using System.Collections.Generic;
using UnityEngine;

/// <summary>Interactableに付ける：狙われた時にアウトラインを表示する</summary>
[RequireComponent(typeof(Interactable))]
public class InteractableHighlight : MonoBehaviour {
    [SerializeField] private Material outlineMaterial;      // Outline.shaderのマテリアル
    [SerializeField] private bool includeChildren = true;   // 子のMeshRendererも対象にするか
    [SerializeField] private bool alwaysOn = false;          // 狙われていなくても常時ハイライトするか

    private List<Renderer> renderers = new List<Renderer>();
    // 各Rendererの元マテリアル配列を保持
    private List<Material[]> originalMaterials = new List<Material[]>();
    private bool isHighlighted = false;

    void Awake() {
        if (includeChildren) {
            renderers.AddRange(GetComponentsInChildren<Renderer>());
        } else {
            var r = GetComponent<Renderer>();
            if (r != null) renderers.Add(r);
        }

        // 元マテリアル配列を保存
        foreach (var r in renderers) {
            originalMaterials.Add(r.sharedMaterials);
        }
    }

    void Start() {
        if (alwaysOn) SetHighlight(true);
    }

    public void SetHighlight(bool on) {
        if (alwaysOn) on = true;
        if (on == isHighlighted) return;
        isHighlighted = on;

        if (outlineMaterial == null) return;

        for (int i = 0; i < renderers.Count; i++) {
            var r = renderers[i];
            if (r == null) continue;

            if (on) {
                // 元のマテリアル配列の末尾にOutlineマテリアルを追加
                var orig = originalMaterials[i];
                var newMats = new Material[orig.Length + 1];
                for (int j = 0; j < orig.Length; j++) newMats[j] = orig[j];
                newMats[orig.Length] = outlineMaterial;
                r.materials = newMats;
            } else {
                // 元に戻す
                r.materials = originalMaterials[i];
            }
        }
    }

    void OnDisable() {
        // 無効化時は必ずハイライト解除
        if (isHighlighted) SetHighlight(false);
    }
}
