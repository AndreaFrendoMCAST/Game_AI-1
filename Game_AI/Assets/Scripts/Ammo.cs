// Scripts/Core/Ammo.cs
using UnityEngine;

public class Ammo : MonoBehaviour
{
    [SerializeField] private int maxAmmo = 30;
    public int MaxAmmo => maxAmmo;
    public int CurrentAmmo { get; private set; }

    private void Awake()
    {
        CurrentAmmo = maxAmmo;
    }

    public void ResetAmmo() => CurrentAmmo = maxAmmo;

    public bool HasAmmo => CurrentAmmo > 0;

    public bool Consume(int amount)
    {
        if (CurrentAmmo < amount) return false;
        CurrentAmmo -= amount;
        return true;
    }

    public void Add(int amount)
    {
        CurrentAmmo = Mathf.Clamp(CurrentAmmo + amount, 0, maxAmmo);
    }

    public float Ammo01 => maxAmmo <= 0 ? 0f : (float)CurrentAmmo / maxAmmo;
}
