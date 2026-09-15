using UnityEngine;

public class WaterRiser : MonoBehaviour {
    [Header("Target")]
    [SerializeField] private Transform waterTransform;  // “®‚©‚·…iWaterVolumej

    [Header("Rising")]
    [SerializeField] private float minutesPerMeter = 10f;  // 1mã¸‚·‚é‚Ì‚É‰½•ª‚©‚©‚é‚©
    [SerializeField] private float maxHeight = 3f;      // ã¸‚ÌãŒÀiƒ[ƒ‹ƒhYj

    void Start() {
        if (waterTransform == null) waterTransform = transform;
    }

    void Update() {
        if (waterTransform == null) return;

        Vector3 pos = waterTransform.position;

        // ãŒÀ“ž’B‚Å’âŽ~
        if (pos.y >= maxHeight) return;

        // 1•ª = 60•bB 1m / (minutesPerMeter * 60•b) = 1•b‚ ‚½‚è‚Ìã¸—Ê[m]
        float riseSpeed = 1f / (minutesPerMeter * 60f);
        pos.y += riseSpeed * Time.deltaTime;

        if (pos.y > maxHeight) pos.y = maxHeight;
        waterTransform.position = pos;
    }
}
