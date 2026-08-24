using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BossTrigger : MonoBehaviour
{
    [Header("起動対象のボス")]
    [SerializeField] private BossMove boss;

    private void OnTriggerEnter(Collider other)
    {
        // プレイヤーが侵入したか判定
        if (other.GetComponent<Player>() != null)
        {
            if (boss != null)
            {
                // ボスを起動
                boss.gameObject.SetActive(true);
                boss.ActivateBoss();
            }

            // 1度起動したらトリガーオブジェクトを無効化
            gameObject.SetActive(false);
        }
    }
}