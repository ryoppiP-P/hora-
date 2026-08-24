using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class InteractableHighlight : MonoBehaviour {
    [Header("Materials")]
    [SerializeField] private Material outlineMaterial;      // 狙われた時（通常の強さ）
    [SerializeField] private Material dimOutlineMaterial;   // 狙われていない時（弱め）

    [Header("Settings")]
    [SerializeField] private bool includeChildren = true;
    [SerializeField] private bool alwaysOn = false;         // trueなら常時dim、狙われたらstrongに切替

    private List<Renderer> renderers = new List<Renderer>();
    private List<Material[]> originalMaterials = new List<Material[]>();

    // 現在の適用状態：0=なし, 1=弱, 2=強
    private int currentLevel = 0;

    void Awake() {
        if (includeChildren) {
            renderers.AddRange(GetComponentsInChildren<Renderer>());
        } else {
            var r = GetComponent<Renderer>();
            if (r != null) renderers.Add(r);
        }

        foreach (var r in renderers) {
            originalMaterials.Add(r.sharedMaterials);
        }
    }

    void Start() {
        // AlwaysOnなら最初から弱めのハイライトを出す
        if (alwaysOn) ApplyLevel(1);
    }

    /// <summary>Interactor から呼ばれる：狙われた=true、外れた=false</summary>
    public void SetHighlight(bool on) {
        // AlwaysOn時：狙われたら強、外れたら弱に戻る
        // 通常時：狙われたら強、外れたら消える
        int target;
        if (alwaysOn) {
            target = on ? 2 : 1;
        } else {
            target = on ? 2 : 0;
        }
        ApplyLevel(target);
    }

    private void ApplyLevel(int level) {
        if (level == currentLevel) return;
        currentLevel = level;

        Material matToApply = null;
        if (level == 1) matToApply = dimOutlineMaterial;
        else if (level == 2) matToApply = outlineMaterial;

        for (int i = 0; i < renderers.Count; i++) {
            var r = renderers[i];
            if (r == null) continue;

            if (matToApply == null) {
                // 元に戻す
                r.materials = originalMaterials[i];
            } else {
                var orig = originalMaterials[i];
                var newMats = new Material[orig.Length + 1];
                for (int j = 0; j < orig.Length; j++) newMats[j] = orig[j];
                newMats[orig.Length] = matToApply;
                r.materials = newMats;

                // 元のテクスチャをアウトライン側にも渡す（_MainTexが無いシェーダー(Arnold等)は対象外にして警告を防ぐ）
                if (orig.Length > 0 && orig[0] != null && orig[0].HasProperty("_MainTex") && orig[0].mainTexture != null) {
                    var mpb = new MaterialPropertyBlock();
                    r.GetPropertyBlock(mpb, orig.Length);
                    mpb.SetTexture("_MainTex", orig[0].mainTexture);
                    r.SetPropertyBlock(mpb, orig.Length);
                }
            }
        }
    }

    void OnDisable() {
        if (currentLevel != 0) ApplyLevel(0);
    }
}
