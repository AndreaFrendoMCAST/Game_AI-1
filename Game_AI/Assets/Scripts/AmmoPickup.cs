// Scripts/Pickups/AmmoPickup.cs
using UnityEngine;

public class AmmoPickup : PickupBase
{
    [SerializeField] private int ammoAmount = 10;
    [SerializeField] private bool respawn = true;
    [SerializeField] private float respawnDelay = 8f;

    public override void Apply(GameObject picker)
    {
        var ammo = picker.GetComponentInParent<Ammo>();
        if (ammo == null) return;

        ammo.Add(ammoAmount);

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
