// Scripts/Pickups/HealthPickup.cs
using UnityEngine;

public class HealthPickup : PickupBase
{
    [SerializeField] private float healAmount = 40f;
    [SerializeField] private bool respawn = true;
    [SerializeField] private float respawnDelay = 8f;

    public override void Apply(GameObject picker)
    {
        var h = picker.GetComponentInParent<Health>();
        if (h == null || h.IsDead) return;

        h.Heal(healAmount);

        if (respawn) StartCoroutine(Respawn());
        else Destroy(gameObject);
    }

    private System.Collections.IEnumerator Respawn()
    {
        var col = GetComponent<Collider>();
        var rend = GetComponentInChildren<Renderer>();

        if (col) col.enabled = false;
        if (rend) rend.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        if (col) col.enabled = true;
        if (rend) rend.enabled = true;
    }
}
