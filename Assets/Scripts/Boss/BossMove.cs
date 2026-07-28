using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class BossMove : MonoBehaviour
{
    public enum AIState
    {
        Patrol,      // ランダム巡回
        Investigate, // 音の聞こえた場所へ移動
        Alert        // 到着して周囲を警戒
    }

    [Header("AI状態")]
    [SerializeField] private AIState currentState = AIState.Patrol;

    [Header("移動速度設定")]
    [SerializeField] private float patrolSpeed = 3.5f;      // 巡回時の移動速度
    [SerializeField] private float investigateSpeed = 6.0f;  // 音検知時の移動速度

    [Header("巡回設定")]
    [SerializeField] private float patrolRadius = 15f;        // 巡回エリアの半径
    [SerializeField] private float patrolWaitTime = 1f;        // 到着後の待機時間

    [Header("聴覚設定")]
    [SerializeField] private float maxHearingDistance = 20f;  // 音が届く限界距離
    [SerializeField] private float hearThreshold = 0.05f;     // 感知に必要な最小音量
    [SerializeField] private bool useDirectionTarget = true;  // true: 音が聞こえた角へ向かう / false: 音源の位置へ直行

    [Header("警戒設定")]
    [SerializeField] private float alertDuration = 3f;        // 音の場所で警戒する時間（秒）

    private NavMeshAgent agent;
    private float timer;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void OnEnable()
    {
        SoundSystem.OnSound += HandleSound;
    }

    private void OnDisable()
    {
        SoundSystem.OnSound -= HandleSound;
    }

    private void Start()
    {
        // 初期状態
        agent.speed = patrolSpeed;
        SetNextRandomDestination();
    }

    private void Update()
    {
        switch (currentState)
        {
            case AIState.Patrol:
                UpdatePatrol();
                break;

            case AIState.Investigate:
                UpdateInvestigate();
                break;

            case AIState.Alert:
                UpdateAlert();
                break;
        }
    }

    // ============================================
    // 各状態の更新処理
    // ============================================
    private void UpdatePatrol()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            timer += Time.deltaTime;
            if (timer >= patrolWaitTime)
            {
                SetNextRandomDestination();
                timer = 0f;
            }
        }
    }

    private void UpdateInvestigate()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            currentState = AIState.Alert;
            timer = 0f;
        }
    }

    private void UpdateAlert()
    {
        timer += Time.deltaTime;

        if (timer >= alertDuration)
        {
            // 速度を巡回用に変更
            currentState = AIState.Patrol;
            agent.speed = patrolSpeed;
            timer = 0f;
            SetNextRandomDestination();
        }
    }

    // ============================================
    // 音を受信したときの処理
    // ============================================
    private void HandleSound(SoundInfo info)
    {
        if (info.source == gameObject) return;

        if (SoundPropagation.TryHear(
            transform.position,
            info.position,
            info.loudness,
            maxHearingDistance,
            out float perceived,
            out Vector3 directionTarget))
        {
            if (perceived >= hearThreshold)
            {
                Vector3 targetPosition = useDirectionTarget ? directionTarget : info.position;

                // 音に反応したら目的地へ設定
                agent.speed = investigateSpeed;
                agent.SetDestination(targetPosition);
                currentState = AIState.Investigate;
                timer = 0f;
            }
        }
    }

    // ============================================
    // 補助メソッド
    // ============================================
    private void SetNextRandomDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += transform.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, patrolRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, maxHearingDistance);
    }
}