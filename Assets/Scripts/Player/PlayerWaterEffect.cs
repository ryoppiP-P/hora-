using UnityEngine;
using System;

public class PlayerWaterEffect : MonoBehaviour {
    [SerializeField] private CharacterController controller;
    [SerializeField] private Player player;

    [Header("Water Drag")]
    [SerializeField] private float minSpeedMultiplier = 0.3f; // 完全水没時の速度倍率
    [SerializeField] private AnimationCurve dragCurve = AnimationCurve.EaseInOut(0, 1, 1, 0.3f);

    [Header("Drown")]
    [SerializeField] private float drownThreshold = 0.95f; // この浸水率で死亡

    private WaterVolume currentWater;
    private bool isDead = false;

    // 現在の浸水率（0 ~ 1）
    public float SubmergeRatio { get; private set; }
    // 速度倍率（外部から参照する用）
    public float SpeedMultiplier { get; private set; } = 1f;
    public bool IsDead => isDead;

    // 他のシステムから購読できるように
    public event Action OnDrown;

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
        if (isDead) return;

        if (currentWater == null) {
            SubmergeRatio = 0f;
            SpeedMultiplier = 1f;
            return;
        }

        // プレイヤーの底面と頂点のY座標
        float playerBottom = controller.bounds.min.y;
        float playerTop = controller.bounds.max.y;
        float waterY = currentWater.GetWaterSurfaceY();

        // 浸水率を計算
        float submerged = waterY - playerBottom;       // 水面より下にある身体の高さ
        float playerHeight = playerTop - playerBottom;
        SubmergeRatio = Mathf.Clamp01(submerged / playerHeight);

        // カーブで速度倍率に変換（直線でもいいけどカーブのほうが自然）
        SpeedMultiplier = dragCurve.Evaluate(SubmergeRatio);

        // 死亡判定
        if (SubmergeRatio >= drownThreshold) {
            Drown();
        }
    }

    void Drown() {
        isDead = true;
        Debug.Log("プレイヤーは溺死した");
        OnDrown?.Invoke();
    }
}
