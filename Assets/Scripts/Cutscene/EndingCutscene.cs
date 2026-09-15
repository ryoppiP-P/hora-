using System.Collections;
using UnityEngine;

public class EndingCutscene : MonoBehaviour {
    [Header("References")]
    public Player player;
    public CameraLook cameraLook;
    public Transform podEntryPoint;   // ドアの正面（船の外）
    public Transform podInsidePoint;  // 船内の到達位置
    public ClearObject clearObject;   // 最後にドアを閉めるのに使う

    [Header("Timing")]
    public float walkToEntryDuration = 1.6f;   // ドアの正面まで回り込む
    public float walkInDuration = 2.0f;        // 開口部をくぐって船内へ
    public float pauseBeforeTurn = 0.4f;
    public float turnDuration = 1.4f;          // 振り返る
    public float pauseBeforeDoorClose = 0.4f;
    public float doorCloseTimeout = 3.0f;      // ドアが閉じ切るのを待つ上限
    public float pauseAfterClose = 1.0f;

    [SerializeField] private GameManager gameManager;

    public IEnumerator Play() {
        // 入力ロック & 無敵化
        if (player != null) {
            player.SetInputLocked(true);
            player.SetInvincible(true);
        }
        if (cameraLook != null) cameraLook.cutsceneControlled = true;

        // Rigidbodyを止める（Playerが物理制御なら）
        var rb = player.GetComponent<Rigidbody>();
        if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }

        float yaw = cameraLook.GetRotation().yaw;

        // ① ドアの正面まで回り込む。着いた時点でドアの方を向いているようにする
        if (podEntryPoint != null) {
            float yawFacingDoor = YawToward(podEntryPoint.position, podInsidePoint.position);
            yield return MoveAndTurn(podEntryPoint.position, yaw, yawFacingDoor, walkToEntryDuration);
            yaw = yawFacingDoor;
        }

        // ② 開口部をくぐって船内へ（向きはそのまま）
        yield return MoveAndTurn(podInsidePoint.position, yaw, yaw, walkInDuration);

        yield return new WaitForSeconds(pauseBeforeTurn);

        // ③ 振り返ってドアの方を見る
        float yawBack = podEntryPoint != null
            ? YawToward(podInsidePoint.position, podEntryPoint.position)
            : yaw + 180f;
        yield return TurnTo(yaw, yawBack, turnDuration);

        yield return new WaitForSeconds(pauseBeforeDoorClose);

        // ④ 目の前でドアが閉まる
        if (clearObject != null) {
            clearObject.CloseHatch();
            float waited = 0f;
            while (waited < doorCloseTimeout && !clearObject.IsHatchClosed) {
                waited += Time.deltaTime;
                yield return null;
            }
        }

        yield return new WaitForSeconds(pauseAfterClose);

        // ⑤ クリア画面へ（GameManagerに委譲）
        gameManager.TriggerClear();
    }

    /// <summary>fromからtoを見る時のyaw角度</summary>
    private float YawToward(Vector3 from, Vector3 to) {
        Vector3 dir = to - from;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return cameraLook.GetRotation().yaw;
        return Quaternion.LookRotation(dir).eulerAngles.y;
    }

    /// <summary>現在地から目標地点へ移動しつつ、向きも補間する</summary>
    private IEnumerator MoveAndTurn(Vector3 target, float yawFrom, float yawTo, float duration) {
        Vector3 start = player.transform.position;
        float t = 0f;
        while (t < duration) {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            player.transform.position = Vector3.Lerp(start, target, k);
            cameraLook.SetRotation(0f, Mathf.LerpAngle(yawFrom, yawTo, k));
            yield return null;
        }
        player.transform.position = target;
        cameraLook.SetRotation(0f, yawTo);
    }

    /// <summary>その場で向きだけ変える</summary>
    private IEnumerator TurnTo(float yawFrom, float yawTo, float duration) {
        float t = 0f;
        while (t < duration) {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            cameraLook.SetRotation(0f, Mathf.LerpAngle(yawFrom, yawTo, k));
            yield return null;
        }
        cameraLook.SetRotation(0f, yawTo);
    }
}
