// Scripts/AI/Guard/GuardAI.cs
using UnityEngine;
using UnityEngine.AI;

public class GuardAI : MonoBehaviour
{
    private enum State { Patrol, Chase, Search }

    [Header("Refs")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Perception perception;
    [SerializeField] private SimpleWeapon weapon;

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolSpeed = 3.5f;
    [SerializeField] private float waypointTolerance = 1.2f;

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 5.0f;

    [Header("Search")]
    [SerializeField] private float searchSpeed = 3.75f;
    [SerializeField] private float searchDuration = 3.0f;

    private State _state = State.Patrol;
    private int _patrolIndex;
    private float _searchUntil;

    private void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
        perception = GetComponent<Perception>();
        weapon = GetComponent<SimpleWeapon>();
    }

    private void Update()
    {
        perception.Tick();

        switch (_state)
        {
            case State.Patrol: TickPatrol(); break;
            case State.Chase:  TickChase();  break;
            case State.Search: TickSearch(); break;
        }
    }

    private void TickPatrol()
    {
        agent.speed = patrolSpeed;

        if (perception.HasLineOfSight && perception.CurrentTarget != null)
        {
            _state = State.Chase;
            return;
        }

        if (patrolPoints == null || patrolPoints.Length == 0) return;

        if (!agent.hasPath)
            agent.SetDestination(patrolPoints[_patrolIndex].position);

        if (!agent.pathPending && agent.remainingDistance <= waypointTolerance)
        {
            _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[_patrolIndex].position);
        }
    }

    private void TickChase()
    {
        agent.speed = chaseSpeed;

        if (perception.HasLineOfSight && perception.CurrentTarget != null)
        {
            Vector3 targetPos = perception.CurrentTarget.position;
            agent.SetDestination(targetPos);

            weapon.AimAt(targetPos);
            if (weapon.CanFire) weapon.Fire();
        }
        else
        {
            _state = State.Search;
            _searchUntil = Time.time + searchDuration;
            agent.speed = searchSpeed;
            agent.SetDestination(perception.LastKnownTargetPosition);
        }
    }

    private void TickSearch()
    {
        agent.speed = searchSpeed;

        if (perception.HasLineOfSight && perception.CurrentTarget != null)
        {
            _state = State.Chase;
            return;
        }

        if (Time.time >= _searchUntil)
        {
            _state = State.Patrol;
            agent.ResetPath();
            return;
        }

        // Keep moving to last known position; once there, just wait out the timer.
        if (!agent.pathPending && agent.remainingDistance <= waypointTolerance)
        {
            agent.ResetPath();
        }
    }
}
