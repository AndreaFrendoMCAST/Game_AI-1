// Scripts/Combat/SimpleWeapon.cs
using UnityEngine;

public class SimpleWeapon : MonoBehaviour
{
    [SerializeField] private Transform firePoint;
    [SerializeField] private Projectile projectilePrefab;

    [SerializeField] private float damage = 10f;
    [SerializeField] private float projectileSpeed = 18f;
    [SerializeField] private float fireCooldown = 0.35f;
    [SerializeField] private int ammoCost = 1;

    private float _nextFireTime;
    private Ammo _ammo;

    private void Awake()
    {
        _ammo = GetComponent<Ammo>();
    }

    public bool CanFire => Time.time >= _nextFireTime && (_ammo == null || _ammo.CurrentAmmo >= ammoCost);

    public void Fire()
    {
        if (!CanFire) return;

        if (_ammo != null && !_ammo.Consume(ammoCost)) return;

        _nextFireTime = Time.time + fireCooldown;

        var proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        proj.Init(gameObject, damage, projectileSpeed);
    }

    public void AimAt(Vector3 worldPos)
    {
        Vector3 dir = worldPos - firePoint.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            firePoint.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
    }
}
