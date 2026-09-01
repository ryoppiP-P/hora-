using UnityEngine;

public class Flashlight : MonoBehaviour {
    [Header("References")]
    [SerializeField] private Light flashlight;
    [SerializeField] private Transform followTarget; // 通常はCamera

    [Header("Follow Offset")]
    [SerializeField] private Vector3 positionOffset = Vector3.zero; // 手元寄せ用
    [SerializeField] private bool useLocalOffset = true;             // オフセットをカメラのローカル軸で解釈

    [Header("Follow Easing")]
    [Tooltip("位置追従の速さ（小さいほど遅い）")]
    [SerializeField] private float positionLerpSpeed = 5f;
    [Tooltip("回転追従の速さ（小さいほど遅い）")]
    [SerializeField] private float rotationLerpSpeed = 2.5f;

    [Header("Sway (SmoothDamp)")]
    [Tooltip("Trueで慣性ありのSmoothDamp、Falseで通常のLerp")]
    [SerializeField] private bool useSmoothDamp = true;
    [Tooltip("位置の到達時間(秒)。大きいほど遅い")]
    [SerializeField] private float positionSmoothTime = 0.25f;
    [Tooltip("回転の到達時間(秒)。大きいほど遅い")]
    [SerializeField] private float rotationSmoothTime = 0.35f;
    [Tooltip("最大追従速度（大きすぎる回転に上限をかける）")]
    [SerializeField] private float maxAngularSpeed = 360f;


    // SmoothDamp用の速度キャッシュ
    private Vector3 posVelocity;
    private Vector3 rotEulerVelocity;
    private Vector3 currentEuler;

    void Start() {
        if (followTarget != null) {
            transform.position = followTarget.position;
            transform.rotation = followTarget.rotation;
            currentEuler = transform.eulerAngles;
        }
    }

    void LateUpdate() {
        if (followTarget == null) return;

        // 目標位置
        Vector3 targetPos;
        if (useLocalOffset) {
            targetPos = followTarget.position
                + followTarget.right * positionOffset.x
                + followTarget.up * positionOffset.y
                + followTarget.forward * positionOffset.z;
        } else {
            targetPos = followTarget.position + positionOffset;
        }

        if (useSmoothDamp) {
            // 位置：SmoothDamp（慣性ありでぬるっとした動き）
            transform.position = Vector3.SmoothDamp(
                transform.position, targetPos, ref posVelocity, positionSmoothTime);

            // 回転：Euler角でSmoothDampAngle
            Vector3 targetEuler = followTarget.eulerAngles;
            currentEuler.x = Mathf.SmoothDampAngle(
                currentEuler.x, targetEuler.x, ref rotEulerVelocity.x,
                rotationSmoothTime, maxAngularSpeed);
            currentEuler.y = Mathf.SmoothDampAngle(
                currentEuler.y, targetEuler.y, ref rotEulerVelocity.y,
                rotationSmoothTime, maxAngularSpeed);
            currentEuler.z = Mathf.SmoothDampAngle(
                currentEuler.z, targetEuler.z, ref rotEulerVelocity.z,
                rotationSmoothTime, maxAngularSpeed);
            transform.eulerAngles = currentEuler;
        } else {
            // 通常のLerp
            float posT = 1f - Mathf.Exp(-positionLerpSpeed * Time.deltaTime);
            float rotT = 1f - Mathf.Exp(-rotationLerpSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, targetPos, posT);
            transform.rotation = Quaternion.Slerp(transform.rotation, followTarget.rotation, rotT);
        }
    }
}
