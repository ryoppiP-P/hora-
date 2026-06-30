using UnityEngine;

public class WaterVolume : MonoBehaviour {
    [SerializeField] private Collider waterCollider;

    // …–Ê‚ÌYÀ•W‚ğ•Ô‚·
    public float GetWaterSurfaceY() {
        return waterCollider.bounds.max.y;
    }

    // ‚±‚Ì…‚ÌCollider‚ªw’èYÀ•W‚ğŠÜ‚Ş‚©
    public bool ContainsY(float y) {
        var b = waterCollider.bounds;
        return y >= b.min.y && y <= b.max.y;
    }
}
