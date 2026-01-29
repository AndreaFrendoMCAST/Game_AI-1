// Scripts/Core/Perception.cs
using UnityEngine;

public class Perception : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float detectionRadius = 15f;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private LayerMask occlusionLayers;

    [Header("Output")]
    public Transform CurrentTarget { get; private set; }
    public Vector3 LastKnownTargetPosition { get; private set; }
    public bool HasLineOfSight { get; private set; }

    private AgentIdentity _id;

    private void Awake()
    {
        _id = GetComponent<AgentIdentity>();
    }

    public void Tick()
    {
        CurrentTarget = null;
        HasLineOfSight = false;

        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, targetLayers, QueryTriggerInteraction.Ignore);

        float bestDist = float.MaxValue;
        Transform best = null;

        foreach (var h in hits)
        {
            if (!h.transform) continue;
            if (h.transform == transform) continue;

            var otherId = h.GetComponentInParent<AgentIdentity>();
            if (otherId != null && _id != null && otherId.team == _id.team) continue;

            var otherHealth = h.GetComponentInParent<Health>();
            if (otherHealth != null && otherHealth.IsDead) continue;

            Vector3 origin = transform.position + Vector3.up * 1.2f;
            Vector3 dest = h.transform.position + Vector3.up * 1.2f;
            Vector3 dir = (dest - origin);
            float dist = dir.magnitude;

            if (dist < bestDist)
            {
                if (!Physics.Raycast(origin, dir.normalized, dist, occlusionLayers, QueryTriggerInteraction.Ignore))
                {
                    bestDist = dist;
                    best = h.transform;
                }
            }
        }

        if (best != null)
        {
            CurrentTarget = best;
            HasLineOfSight = true;
            LastKnownTargetPosition = best.position;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
