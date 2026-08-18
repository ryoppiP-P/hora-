using System.Collections;
using UnityEngine;

public class EndingCutscene : MonoBehaviour {
    [Header("References")]
    public Player player;
    public CameraLook cameraLook;
    public Transform podInsidePoint;  // ポッド内部の到達位置（空GameObject）

    [Header("Timing")]
    public float walkInDuration = 2.5f;
    public float pauseBeforeTurn = 0.5f;
    public float turnDuration = 2.0f;
    public float pauseAfterTurn = 1.0f;

    [SerializeField]private GameManager gameManager;

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

        // ① ポッド内部までゆっくり歩いて入る
        Vector3 startPos = player.transform.position;
        Vector3 endPos = podInsidePoint.position;
        float startYaw = cameraLook.GetRotation().yaw;

        float t = 0f;
        while (t < walkInDuration) {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / walkInDuration);
            player.transform.position = Vector3.Lerp(startPos, endPos, k);
            yield return null;
        }
        player.transform.position = endPos;

        yield return new WaitForSeconds(pauseBeforeTurn);

        // ② 180度振り返る
        float targetYaw = startYaw + 180f;
        t = 0f;
        while (t < turnDuration) {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / turnDuration);
            cameraLook.SetRotation(0f, Mathf.Lerp(startYaw, targetYaw, k));
            yield return null;
        }
        cameraLook.SetRotation(0f, targetYaw);

        yield return new WaitForSeconds(pauseAfterTurn);

        // ③ クリア画面へ（GameManagerに委譲）
        gameManager.TriggerClear();
    }
}
