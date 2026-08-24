using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class BossMove : MonoBehaviour
{
    public enum AIState
    {
        Inactive,    // 通路を通るまでの待機状態（起動前）/ ポッド起動後の完全停止
        Patrol,      // ランダム巡回
        Investigate, // 音の聞こえた場所へ移動
        Alert,       // 到着して周囲を警戒
        Attack       // プレイヤーを攻撃
    }

    [Header("AI状態")]
    [SerializeField] private AIState currentState = AIState.Inactive; // 初期状態（デフォルトは待機）
    [SerializeField] private bool startsActive = false;               // 最初から動かしたいシーン用のフラグ

    [Header("脱出ポッド設定")]
    [SerializeField] private ClearObject clearObject;                 // 脱出ポッドの参照（OnActivated購読用）

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

    [Header("攻撃設定")]
    [SerializeField] private Player player;                  // プレイヤー参照
    [SerializeField] private float attackTriggerDistance = 0.5f;  // Attackに入る距離
    [SerializeField] private float attackWindupTime = 0.5f;       // 攻撃発動までの溜め時間
    [SerializeField] private float attackKillDistance = 1.5f;     // この距離以内なら殺せる

    private NavMeshAgent agent;
    private float timer;
    private float attackTimer = 0f;
    private bool isPodActivated = false; // 脱出ポッドが起動済みかどうかのフラグ

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void OnEnable()
    {
        // イベントの購読開始
        SoundSystem.OnSound += HandleSound;

        // 脱出ポッドのアクティベートイベントを購読
        if (clearObject != null)
        {
            clearObject.OnActivated += HandlePodActivated;
        }
    }

    private void OnDisable()
    {
        // オブジェクト非アクティブ時
        SoundSystem.OnSound -= HandleSound;

        // 脱出ポッドのアクティベートイベント購読解除
        if (clearObject != null)
        {
            clearObject.OnActivated -= HandlePodActivated;
        }
    }

    private void Start()
    {
        // 初期状態の速度をセット
        agent.speed = patrolSpeed;

        // 最初から動かすフラグが立っている場合は起動
        if (startsActive)
        {
            ActivateBoss();
        }
    }

    private void Update()
    {
        // 待機状態、またはポッド起動後なら移動・攻撃の判定を行わない
        if (currentState == AIState.Inactive || isPodActivated) return;

        // Attack以外の状態のとき、プレイヤーが近ければAttackへ遷移
        if (currentState != AIState.Attack && player != null && !player.IsDead)
        {
           
            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist <= attackTriggerDistance)
            {
               
                EnterAttack();
            }
        }

        switch (currentState) {
            case AIState.Patrol:
           
                UpdatePatrol();
                break;
        case AIState.Investigate:
           
                UpdateInvestigate();
                break;
        case AIState.Alert:
           
                UpdateAlert();
                break;
        case AIState.Attack:
           
                UpdateAttack();
                break;
        }
    }

    // ============================================
    // 脱出ポッド起動時の処理（オブザーバー受信）
    // ============================================
    private void HandlePodActivated()
    {
        isPodActivated = true;
        currentState = AIState.Inactive;

        // NavMeshAgentを停止して現在の経路をクリア
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    // ============================================
    // ボス起動処理（トリガーから呼び出し）
    // ============================================
    public void ActivateBoss()
    {
        // ポッド起動後や、既に動いている場合は実行しない
        if (isPodActivated) return;
        if (currentState != AIState.Inactive && startsActive) return;

        currentState = AIState.Patrol;
        SetNextRandomDestination();
    }

    // ============================================
    // 各状態への遷移処理
    // ============================================
    private void EnterAttack()
    {
       
        currentState = AIState.Attack;
        attackTimer = 0f;

        // 停止
        agent.isStopped = true;
        agent.ResetPath();
    }

    // ============================================
    // 各状態の更新処理
    // ============================================
    private void UpdatePatrol()
    {
        // 目的地に到達したか判定
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
        // 音の場所に到達したら警戒状態へ移行
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
            // 警戒終了後、速度を巡回用に戻してパトロール再開
            currentState = AIState.Patrol;
            agent.speed = patrolSpeed;
            timer = 0f;
            SetNextRandomDestination();
        }
    }

    private void UpdateAttack()
    {
       
        if (player == null || player.IsDead) return;

        attackTimer += Time.deltaTime;

        // 溜め時間経過で判定
        if (attackTimer >= attackWindupTime)
        {
           
            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist <= attackKillDistance)
            {
               
                player.Kill();
            }

            // 攻撃終了、Alertへ移行して警戒状態に
            agent.isStopped = false;
            currentState = AIState.Alert;
            timer = 0f;
        }
    }

    // ============================================
    // 音を受信したときの処理
    // ============================================
    private void HandleSound(SoundInfo info)
    {
        // 待機状態またはポッド起動後なら音に反応しない
        if (currentState == AIState.Inactive || isPodActivated) return;

        // 自身が出した音なら無視
        if (info.source == gameObject) return;

        // 音が聞こえるか計算
        if (SoundPropagation.TryHear(
            transform.position,
            info.position,
            info.loudness,
            maxHearingDistance,
            out float perceived,
            out Vector3 directionTarget))
        {
            // 減衰後の音量が設定した閾値を超えているか確認
            if (perceived >= hearThreshold)
            {
                // 音の発生源または聞こえてくる角の位置を取得
                Vector3 targetPosition = useDirectionTarget ? directionTarget : info.position;

                // 音に反応したら移動速度を変更して目的地へ設定
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
        // 巡回範囲内のランダムな座標を計算
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += transform.position;

        NavMeshHit hit;
        // NavMesh上の有効な座標を取得
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 巡回範囲の可視化（黄色）
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, patrolRadius);

        // 聴覚範囲の可視化（青色）
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, maxHearingDistance);
    }
}