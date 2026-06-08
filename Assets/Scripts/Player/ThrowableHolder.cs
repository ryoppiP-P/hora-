using UnityEngine;
using UnityEngine.InputSystem;

public class ThrowableHolder : MonoBehaviour {
    [SerializeField] private Player player;
    [SerializeField] private Camera cam;
    [SerializeField] private Transform holdPoint;       // カメラ子の手元位置

    [Header("Throw")]
    [SerializeField] private float maxRange = 15f;      // 最大15m
    [SerializeField] private float minThrowSpeed = 6f;  // 構えた瞬間の最低速度
    [SerializeField] private float chargeTime = 0.8f;   // 構え完了までの秒数
    [SerializeField] private float throwAngleDeg = 15f; // 上向き角度

    [Header("Trajectory")]
    [SerializeField] private LineRenderer trajectoryLine;
    [SerializeField] private int trajectorySegments = 30;
    [SerializeField] private float trajectoryStep = 0.05f;

    private Throwable held;
    private bool isAiming;
    private float aimTimer;

    public bool HasItem => held != null;
    public bool IsAiming => isAiming;

    public void Pickup(Throwable t) {
        // 既存があれば入れ替え（足元に落とす）
        if (held != null) {
            held.SetHeld(false);
            held.transform.position = holdPoint.position;
        }
        held = t;
        held.SetHeld(true, holdPoint);
    }

    void Update() {
        var mouse = Mouse.current;
        if (mouse == null) return;

        // 構え（右クリック長押し）
        if (held != null && mouse.rightButton.isPressed) {
            isAiming = true;
            aimTimer = Mathf.Min(aimTimer + Time.deltaTime, chargeTime);
        }
        else if (isAiming && mouse.rightButton.wasReleasedThisFrame) {
            // 離した瞬間に投擲
            ThrowHeld();
            isAiming = false;
            aimTimer = 0f;
        }
        else {
            isAiming = false;
            aimTimer = 0f;
        }

        // 軌道予測UI
        if (trajectoryLine != null) {
            trajectoryLine.enabled = isAiming && held != null;
            if (trajectoryLine.enabled)
                DrawTrajectory(GetThrowOrigin(), GetThrowVelocity());
        }
    }

    Vector3 GetThrowOrigin() => holdPoint.position;

    Vector3 GetThrowVelocity() {
        // チャージ率で速度を上げる（最低速度 最大15m射程に届く速度）
        float t = (chargeTime <= 0f) ? 1f : Mathf.Clamp01(aimTimer / chargeTime);

        // 最大15mに上向き15度で届く初速を計算
        // R = v^2 * sin(2θ) / g より v = sqrt(R*g / sin(2θ))
        float g = -Physics.gravity.y;
        float thetaRad = throwAngleDeg * Mathf.Deg2Rad;
        float vMax = Mathf.Sqrt(maxRange * g / Mathf.Sin(2f * thetaRad));
        float v = Mathf.Lerp(minThrowSpeed, vMax, t);

        // カメラ前方を水平基準に、上向き角度を加える
        Vector3 fwd = cam.transform.forward;
        Vector3 horiz = new Vector3(fwd.x, 0f, fwd.z).normalized;
        Vector3 dir = Quaternion.AngleAxis(-throwAngleDeg, cam.transform.right) * horiz;
        // ↑水平→上向きに回転（カメラのrightを軸に上方向へ）

        return dir * v;
    }

    void ThrowHeld() {
        if (held == null) return;
        Vector3 v = GetThrowVelocity();
        held.transform.position = GetThrowOrigin();
        held.Throw(v);
        held = null;
    }

    void DrawTrajectory(Vector3 origin, Vector3 velocity) {
        Vector3[] points = new Vector3[trajectorySegments];
        Vector3 g = Physics.gravity;
        for (int i = 0; i < trajectorySegments; i++) {
            float t = i * trajectoryStep;
            points[i] = origin + velocity * t + 0.5f * g * t * t;
        }
        trajectoryLine.positionCount = trajectorySegments;
        trajectoryLine.SetPositions(points);
    }
}
