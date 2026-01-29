// Scripts/Pickups/PickupBase.cs
using System.Collections.Generic;
using UnityEngine;

public abstract class PickupBase : MonoBehaviour
{
    private static readonly List<PickupBase> All = new();

    protected virtual void OnEnable() => All.Add(this);
    protected virtual void OnDisable() => All.Remove(this);

    public static T FindNearest<T>(Vector3 from) where T : PickupBase
    {
        float best = float.MaxValue;
        T bestPickup = null;

        foreach (var p in All)
        {
            if (p is not T t) continue;
            float d = (t.transform.position - from).sqrMagnitude;
            if (d < best)
            {
                best = d;
                bestPickup = t;
            }
        }
        return bestPickup;
    }

    public abstract void Apply(GameObject picker);

    private void OnTriggerEnter(Collider other)
    {
        var health = other.GetComponentInParent<Health>();
        if (health == null || health.IsDead) return;

        Apply(other.gameObject);
    }
}
