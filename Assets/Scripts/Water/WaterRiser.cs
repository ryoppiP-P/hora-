using UnityEngine;

public class WaterRiser : MonoBehaviour {
    [Header("Target")]
    [SerializeField] private Transform waterTransform;  // 動かす水（WaterVolume）

    [Header("Rising")]
    [SerializeField] private float riseSpeed = 0.05f;   // 1秒あたりの上昇量（m/s）
    [SerializeField] private float maxHeight = 3f;      // 上昇の上限（ワールドY）

    void Start() {
        if (waterTransform == null) waterTransform = transform;
    }

    void Update() {
        if (waterTransform == null) return;

        Vector3 pos = waterTransform.position;

        // 上限到達で停止
        if (pos.y >= maxHeight) return;

        pos.y += riseSpeed * Time.deltaTime;
        if (pos.y > maxHeight) pos.y = maxHeight;

        waterTransform.position = pos;
    }
}
