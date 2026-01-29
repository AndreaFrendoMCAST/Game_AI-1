// Scripts/AI/Strategist/StrategistAI.cs
using UnityEngine;
using UnityEngine.AI;

public class StrategistAI : MonoBehaviour
{
    private enum ActionType { None, Engage, Heal, CollectAmmo, Flee }

    [Header("Refs")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Perception perception;
    [SerializeField] private SimpleWeapon weapon;
    [SerializeField] private Health health;
    [SerializeField] private Ammo ammo;

    [Header("Decision Timing")]
    [SerializeField] private float decisionInterval = 0.5f;
    [SerializeField] private float actionLockTime = 1.0f; // prevents rapid flip-flop
    private float _nextDecisionTime;
    private float _lockedUntil;

    [Header("Hierarchical Goal: Survival Override")]
    [SerializeField] private float criticalHealth01 = 0.25f;

    [Header("Nav Speeds")]
    [SerializeField] private float normalSpeed = 4.0f;
    [SerializeField] private float fleeSpeed = 5.5f;

    [Header("Utility Weights")]
    [SerializeField] private float engageWeight = 1.0f;
    [SerializeField] private float healWeight = 1.25f;
    [SerializeField] private float ammoWeight = 0.8f;
    [SerializeField] private float fleeWeight = 1.2f;

    [Header("Flee")]
    [SerializeField] private float fleeDistance = 10f;

    private ActionType _currentAction = ActionType.None;
    private ActionType _lastAction = ActionType.None;

    private void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
        perception = GetComponent<Perception>();
        weapon = GetComponent<SimpleWeapon>();
        health = GetComponent<Health>();
        ammo = GetComponent<Ammo>();
    }

    private void Update()
    {
        perception.Tick();

        if (Time.time >= _nextDecisionTime)
        {
            _nextDecisionTime = Time.time + decisionInterval;
            Decide();
        }

        ExecuteCurrentAction();
    }

    private void Decide()
    {
        if (Time.time < _lockedUntil) return;

        // 2.2 Hierarchical Goal Management: Survival overrides utility
        if (health.Health01 <= criticalHealth01)
        {
            SetAction(SurvivalPlan());
            return;
        }

        // 2.1 Utility scoring every interval (multiple actions evaluated)
        float engage = ScoreEngage() * engageWeight;
        float heal = ScoreHeal() * healWeight;
        float collectAmmo = ScoreCollectAmmo() * ammoWeight;
        float flee = ScoreFlee() * fleeWeight;

        // Never perform the same action twice in a row (brief requirement)
        // If best equals lastAction, we choose next best.
        ActionType chosen = BestOf(
            (ActionType.Engage, engage),
            (ActionType.Heal, heal),
            (ActionType.CollectAmmo, collectAmmo),
            (ActionType.Flee, flee)
        );

        if (chosen == _lastAction)
        {
            chosen = SecondBestOf(
                (ActionType.Engage, engage),
                (ActionType.Heal, heal),
                (ActionType.CollectAmmo, collectAmmo),
                (ActionType.Flee, flee),
                _lastAction
            );
        }

        SetAction(chosen);
    }

    private ActionType SurvivalPlan()
    {
        // If a health pickup exists, go heal. If not, flee.
        var hp = PickupBase.FindNearest<HealthPickup>(transform.position);
        if (hp != null) return ActionType.Heal;
        return ActionType.Flee;
    }

    private void SetAction(ActionType next)
    {
        if (next == _currentAction) return;

        _lastAction = _currentAction;
        _currentAction = next;
        _lockedUntil = Time.time + actionLockTime;

        // Clear path so actions feel distinct
        agent.ResetPath();
    }

    private void ExecuteCurrentAction()
    {
        agent.speed = normalSpeed;

        switch (_currentAction)
        {
            case ActionType.Engage: ExecuteEngage(); break;
            case ActionType.Heal: ExecuteHeal(); break;
            case ActionType.CollectAmmo: ExecuteCollectAmmo(); break;
            case ActionType.Flee: ExecuteFlee(); break;
        }
    }

    private void ExecuteEngage()
    {
        if (!perception.HasLineOfSight || perception.CurrentTarget == null)
        {
            // If no target, drift toward last known position to look "smart"
            agent.SetDestination(perception.LastKnownTargetPosition);
            return;
        }

        Vector3 targetPos = perception.CurrentTarget.position;

        // Maintain distance a bit (optional)
        agent.SetDestination(targetPos);

        weapon.AimAt(targetPos);
        if (weapon.CanFire) weapon.Fire();
    }

    private void ExecuteHeal()
    {
        var hp = PickupBase.FindNearest<HealthPickup>(transform.position);
        if (hp == null)
        {
            SetAction(ActionType.Flee);
            return;
        }

        agent.SetDestination(hp.transform.position);
    }

    private void ExecuteCollectAmmo()
    {
        if (ammo != null && ammo.Ammo01 > 0.6f)
        {
            // If we already have enough ammo, prefer engage
            SetAction(ActionType.Engage);
            return;
        }

        var ap = PickupBase.FindNearest<AmmoPickup>(transform.position);
        if (ap == null)
        {
            SetAction(ActionType.Engage);
            return;
        }

        agent.SetDestination(ap.transform.position);
    }

    private void ExecuteFlee()
    {
        agent.speed = fleeSpeed;

        Vector3 threatPos = perception.CurrentTarget != null ? perception.CurrentTarget.position : perception.LastKnownTargetPosition;
        Vector3 away = (transform.position - threatPos);
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f) away = transform.forward;

        Vector3 fleeTarget = transform.position + away.normalized * fleeDistance;

        if (NavMesh.SamplePosition(fleeTarget, out var hit, 4f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    // -------- Utility Scoring --------

    private float ScoreEngage()
    {
        if (!perception.HasLineOfSight || perception.CurrentTarget == null) return 0.1f;

        float hpFactor = Mathf.Clamp01(health.Health01); // higher = more willing
        float ammoFactor = ammo == null ? 1f : Mathf.Clamp01(ammo.Ammo01);
        float dist = Vector3.Distance(transform.position, perception.CurrentTarget.position);
        float distFactor = 1f - Mathf.Clamp01(dist / 20f);

        return (0.45f * hpFactor) + (0.35f * ammoFactor) + (0.20f * distFactor);
    }

    private float ScoreHeal()
    {
        var hp = PickupBase.FindNearest<HealthPickup>(transform.position);
        if (hp == null) return 0f;

        float need = 1f - health.Health01; // lower hp => higher need
        float dist = Vector3.Distance(transform.position, hp.transform.position);
        float distFactor = 1f - Mathf.Clamp01(dist / 25f);

        return (0.7f * need) + (0.3f * distFactor);
    }

    private float ScoreCollectAmmo()
    {
        if (ammo == null) return 0f;

        var ap = PickupBase.FindNearest<AmmoPickup>(transform.position);
        if (ap == null) return 0f;

        float need = 1f - ammo.Ammo01;
        float dist = Vector3.Distance(transform.position, ap.transform.position);
        float distFactor = 1f - Mathf.Clamp01(dist / 25f);

        return (0.7f * need) + (0.3f * distFactor);
    }

    private float ScoreFlee()
    {
        // More likely to flee when low hp or close threat
        float hpNeed = 1f - health.Health01;

        float threatDist = 999f;
        if (perception.CurrentTarget != null)
            threatDist = Vector3.Distance(transform.position, perception.CurrentTarget.position);

        float closeThreat = 1f - Mathf.Clamp01(threatDist / 12f);

        return Mathf.Clamp01(0.6f * hpNeed + 0.4f * closeThreat);
    }

    // -------- Helpers --------

    private static ActionType BestOf(params (ActionType type, float score)[] options)
    {
        ActionType best = ActionType.None;
        float bestScore = float.NegativeInfinity;
        foreach (var o in options)
        {
            if (o.score > bestScore)
            {
                bestScore = o.score;
                best = o.type;
            }
        }
        return best;
    }

    private static ActionType SecondBestOf(
        (ActionType type, float score) a,
        (ActionType type, float score) b,
        (ActionType type, float score) c,
        (ActionType type, float score) d,
        ActionType exclude
    )
    {
        (ActionType type, float score)[] opts = { a, b, c, d };
        // pick best not excluded
        ActionType best = ActionType.None;
        float bestScore = float.NegativeInfinity;

        foreach (var o in opts)
        {
            if (o.type == exclude) continue;
            if (o.score > bestScore)
            {
                bestScore = o.score;
                best = o.type;
            }
        }

        return best == ActionType.None ? exclude : best;
    }
}
