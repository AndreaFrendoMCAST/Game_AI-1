// Scripts/Core/Health.cs
using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }

    public bool IsDead => CurrentHealth <= 0f;

    public event Action<Health> OnDied;
    public event Action<float, GameObject> OnDamaged; // damage, source

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void ResetHealth()
    {
        CurrentHealth = maxHealth;
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0f, maxHealth);
    }

    public void TakeDamage(float amount, GameObject source)
    {
        if (IsDead) return;

        CurrentHealth -= Mathf.Max(0f, amount);
        OnDamaged?.Invoke(amount, source);

        if (CurrentHealth <= 0f)
        {
            CurrentHealth = 0f;
            OnDied?.Invoke(this);
        }
    }

    public float Health01 => maxHealth <= 0f ? 0f : CurrentHealth / maxHealth;
}
