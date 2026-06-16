using UnityEngine;
using UnityEngine.AI;

public class BossMove : MonoBehaviour
{
    [SerializeField]public Transform target; // ’Ç‚¢‚©‚¯‚é‘ŠŽè

    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (target != null)
        {
            agent.SetDestination(target.position);
        }
    }
}