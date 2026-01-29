// Scripts/Combat/Projectile.cs
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 18f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private float damage = 10f;

    private GameObject _owner;
    private float _dieAt;

    public void Init(GameObject owner, float dmg, float projectileSpeed)
    {
        _owner = owner;
        damage = dmg;
        speed = projectileSpeed;
        _dieAt = Time.time + lifeTime;
    }

    private void Update()
    {
        transform.position += transform.forward * (speed * Time.deltaTime);

        if (Time.time >= _dieAt) Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other || other.isTrigger) return;
        if (_owner != null && other.transform.IsChildOf(_owner.transform)) return;

        var health = other.GetComponentInParent<Health>();
        if (health != null && !health.IsDead)
        {
            health.TakeDamage(damage, _owner);

            // Reward owner if it's a GladiatorAgent
            var gladiator = _owner != null ? _owner.GetComponentInParent<GladiatorAgent>() : null;
            if (gladiator != null)
                gladiator.RewardDamageDealt(damage);
        }

        Destroy(gameObject);
    }

}
