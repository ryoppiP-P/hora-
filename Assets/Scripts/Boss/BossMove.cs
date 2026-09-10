using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class BossMove : MonoBehaviour
{
    public enum AIState
    {
        Inactive,    // 隔壁を通るまでの待機状態（起動前）/ ポッド起動後の完全停止
        Patrol,      // ランダム巡回
        Investigate, // 音の方向した場所へ移動
        Alert,       // 見失って周囲を警戒
        Attack       // プレイヤーを攻撃
    }

    [Header("AI状態")]
    [SerializeField] private AIState currentState = AIState.Inactive; // 初期状態（デフォルトは待機）
    [SerializeField] private bool startsActive = false;               // 最初から動き始めるシーン用のフラグ

    [Header("脱出ポッド設定")]
    [SerializeField] private ClearObject clearObject;                 // 脱出ポッドの参照（OnActivated購読用）

    [Header("移動速度設定")]
    [SerializeField] private float patrolSpeed = 3.5f;      // 巡回時の移動速度
    [SerializeField] private float investigateSpeed = 6.0f;  // 音を聞いた時の移動速度

    [Header("巡回設定")]
    [SerializeField] private float patrolRadius = 15f;        // 巡回エリアの半径
    [SerializeField] private float patrolWaitTime = 1f;        // 到着後の待機時間

    [Header("聴覚設定")]
    [SerializeField] private float maxHearingDistance = 20f;  // 音を聞ける限界距離
    [SerializeField] private float hearThreshold = 0.05f;     // 検知に必要な最小音量
    [SerializeField] private bool useDirectionTarget = true;  // true: 音が発生した方角へ向かう / false: 音源の位置へ直行

    [Header("警戒設定")]
    [SerializeField] private float alertDuration = 3f;        // その場所で警戒する時間（秒）

    [Header("攻撃設定")]
    [SerializeField] private Player player;                  // プレイヤー参照
    [SerializeField] private float attackTriggerDistance = 0.5f;  // Attackに入る距離
    [SerializeField] private float attackWindupTime = 0.5f;       // 攻撃が当たるまでの溜め時間
    [SerializeField] private float attackKillDistance = 1.5f;     // この距離以内なら即死

    [Header("ドア強制突破設定")]
    [SerializeField] private float doorBreakRadius = 2.2f; // この範囲内の鍵なしドアを強制的に開け放つ

    [Header("足音設定")]
    [SerializeField] private float footstepWalkInterval = 0.6f; // 巡回時(歩き)の足音間隔(秒)
    [SerializeField] private float footstepRunInterval = 0.4f;  // 音を聞いた時(走り)の足音間隔(秒)

    [Header("うなり声(定期)設定")]
    [SerializeField] private float ambientRoarMinInterval = 25f; // 次のうなり声までの最短時間(秒)
    [SerializeField] private float ambientRoarMaxInterval = 35f; // 次のうなり声までの最長時間(秒)

    [Header("スポーン壁の通過による強制うなり声")]
    [SerializeField] private Collider bossSpawnWallCollider; // BossSpawnWallColliderを割り当てる
    [SerializeField] private float spawnWallRoarDelay = 1f;   // 通過してから鳴くまでの時間(秒)

    private NavMeshAgent agent;
    private float timer;
    private float attackTimer = 0f;
    private float footstepTimer = 0f;
    private float ambientRoarTimer = 0f;
    private bool wasInsideSpawnWall = false;
    private bool hasPassedSpawnWall = false;
    private float spawnWallRoarTimer = -1f; // -1なら未計測。0以上でspawnWallRoarDelayまで加算する
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
        // オブジェクト非アクティブ化
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

        // 最初から動き始めるフラグが立っている場合は起動
        if (startsActive)
        {
            ActivateBoss();
        }
    }

    private void Update()
    {
        // 待機状態、またはポッド起動後なら移動・攻撃の判定を行わない
        if (currentState == AIState.Inactive || isPodActivated) return;

        // Attack以外の状態のとき、プレイヤーが近づいたらAttackへ遷移
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

        UpdateFootstepSE();
        UpdateAmbientRoar();
        UpdateSpawnWallRoar();
        UpdateDoorBreaking();
    }

    // ============================================
    // 脱出ポッド起動時の処理（オブザーバー受信）
    // ============================================
    private void HandlePodActivated()
    {
        isPodActivated = true;
        currentState = AIState.Inactive;

        // NavMeshAgentを止めて現在の経路をクリア
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    // ============================================
    // ボス起動処理（トリガーから呼び出す）
    // ============================================
    public void ActivateBoss()
    {
        // ポッド起動が既に入っている場合は実行しない
        if (isPodActivated) return;
        if (currentState != AIState.Inactive && startsActive) return;

        currentState = AIState.Patrol;
        SetNextRandomDestination();

        // 起動直後から一定時間は無音にせず、25～35秒後に最初のうなり声を出す
        ambientRoarTimer = Random.Range(ambientRoarMinInterval, ambientRoarMaxInterval);
    }

    // ============================================
    // 各状態への遷移処理
    // ============================================
    private void EnterAttack()
    {

        currentState = AIState.Attack;
        attackTimer = 0f;
        Audio.Post("SE.Boss.Roar", transform.position);

        // 停止
        agent.isStopped = true;
        agent.ResetPath();
    }

    // ============================================
    // 各状態の更新処理
    // ============================================
    private void UpdatePatrol()
    {
        // 目的地に到達したら判定
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
        // その場所に到達したら警戒状態へ移行
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
            Audio.Post("SE.Boss.Attack", transform.position);

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
    // 足音・うなり声のSE更新処理
    // ============================================
    private void UpdateFootstepSE()
    {
        if (agent == null || !agent.isOnNavMesh || agent.isStopped)
        {
            footstepTimer = 0f;
            return;
        }

        bool isMoving = agent.velocity.sqrMagnitude > 0.01f;
        if (!isMoving)
        {
            footstepTimer = 0f;
            return;
        }

        // Investigate/Alertの移動速度(investigateSpeed)なら走り、それ以外(Patrol)は歩き扱い
        bool isRunning = agent.speed >= investigateSpeed - 0.01f;
        string key = isRunning ? "SE.Boss.Footstep.Run" : "SE.Boss.Footstep.Walk";
        float interval = isRunning ? footstepRunInterval : footstepWalkInterval;

        footstepTimer -= Time.deltaTime;
        if (footstepTimer <= 0f)
        {
            Audio.Post(key, transform.position);
            footstepTimer = interval;
        }
    }

    private void UpdateAmbientRoar()
    {
        ambientRoarTimer -= Time.deltaTime;
        if (ambientRoarTimer <= 0f)
        {
            Audio.Post("SE.Boss.Roar", transform.position);
            ambientRoarTimer = Random.Range(ambientRoarMinInterval, ambientRoarMaxInterval);
        }
    }

    // BossSpawnWallColliderの範囲を通り過ぎた瞬間を検知し、
    // spawnWallRoarDelay秒後に強制的に一度だけうなり声を鳴らす
    private void UpdateSpawnWallRoar()
    {
        if (bossSpawnWallCollider != null && !hasPassedSpawnWall)
        {
            bool isInside = bossSpawnWallCollider.bounds.Contains(transform.position);
            if (wasInsideSpawnWall && !isInside)
            {
                // 壁の内側から外側へ出た瞬間＝通り過ぎた
                hasPassedSpawnWall = true;
                spawnWallRoarTimer = 0f;
            }
            wasInsideSpawnWall = isInside;
        }

        if (spawnWallRoarTimer >= 0f)
        {
            spawnWallRoarTimer += Time.deltaTime;
            if (spawnWallRoarTimer >= spawnWallRoarDelay)
            {
                Audio.Post("SE.Boss.Roar", transform.position);
                spawnWallRoarTimer = -1f; // 一度だけ。以降は発火しない

                // 直後に定期うなり声が重ならないよう周期を再抽選しておく
                ambientRoarTimer = Random.Range(ambientRoarMinInterval, ambientRoarMaxInterval);
            }
        }
    }

    // 移動中に近くの鍵なしドアがあれば、止まらずに強制的に開け放って進む
    private void UpdateDoorBreaking()
    {
        if (agent == null || !agent.isOnNavMesh || agent.isStopped) return;
        if (agent.velocity.sqrMagnitude < 0.01f) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, doorBreakRadius, ~0, QueryTriggerInteraction.Ignore);
        foreach (var col in hits)
        {
            GrabbableDoor gDoor = col.GetComponentInParent<GrabbableDoor>();
            if (gDoor != null)
            {
                if (!gDoor.IsLocked) gDoor.BossForceOpen(transform.position);
                continue;
            }

            AutoDoor aDoor = col.GetComponentInParent<AutoDoor>();
            if (aDoor != null && !aDoor.IsLocked)
            {
                aDoor.BossForceOpen();
            }
        }
    }

    // ============================================
    // 音を受信した時の処理
    // ============================================
    private void HandleSound(SoundInfo info)
    {
        // 待機状態またはポッド起動後は音に反応しない
        if (currentState == AIState.Inactive || isPodActivated) return;

        // 自分が出した音なら無視
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
                // 音の発生元または方向している角の位置を取得
                Vector3 targetPosition = useDirectionTarget ? directionTarget : info.position;

                // 音に反応した移動速度へ変更して目的地へ設定
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

        // 聴覚範囲の可視化（青）
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, maxHearingDistance);
    }
}
