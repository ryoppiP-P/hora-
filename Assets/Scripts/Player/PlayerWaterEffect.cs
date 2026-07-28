using UnityEngine;

public class PlayerWaterEffect : MonoBehaviour {
    [SerializeField] private CapsuleCollider capsule;    // プレイヤーのCapsuleCollider

    [Header("Water Drag")]
    [SerializeField] private float minSpeedMultiplier = 0.3f; // 完全水没時の速度倍率
    [SerializeField] private AnimationCurve dragCurve = AnimationCurve.EaseInOut(0, 1, 1, 0.3f);    // 浸水率に応じた速度倍率のカーブ

    private WaterVolume currentWater;

    // 現在の浸水率（0 ~ 1）
    public float SubmergeRatio { get; private set; }
    // 速度倍率（外部から参照する用）
    public float SpeedMultiplier { get; private set; } = 1f;
    public bool IsInWater => currentWater != null;

    void OnTriggerEnter(Collider other) {
        var water = other.GetComponent<WaterVolume>();
        if (water != null) currentWater = water;
    }

    void OnTriggerExit(Collider other) {
        var water = other.GetComponent<WaterVolume>();
        if (water != null && water == currentWater) {
            currentWater = null;
            SubmergeRatio = 0f;
            SpeedMultiplier = 1f;
        }
    }

    void Update() {
        if (currentWater == null) {
            SubmergeRatio = 0f;
            SpeedMultiplier = 1f;
            return;
        }

        // プレイヤーの底面と頂点のY座標
        float playerBottom = capsule.bounds.min.y;
        float playerTop = capsule.bounds.max.y;
        float waterY = currentWater.GetWaterSurfaceY();

        // 浸水率を計算
        float submerged = waterY - playerBottom;       // 水面より下にある身体の高さ
        float playerHeight = playerTop - playerBottom;
        SubmergeRatio = Mathf.Clamp01(submerged / playerHeight);

        // カーブで速度倍率に変換
        SpeedMultiplier = dragCurve.Evaluate(SubmergeRatio);
    }
}
