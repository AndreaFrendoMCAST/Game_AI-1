// Scripts/AI/Gladiator/GladiatorAgent.cs
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

[RequireComponent(typeof(Rigidbody))]
public class GladiatorAgent : Agent
{
    [Header("Refs")]
    [SerializeField] private Perception perception;
    [SerializeField] private SimpleWeapon weapon;
    [SerializeField] private Health health;
    [SerializeField] private Ammo ammo;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float turnSpeed = 180f;

    [Header("Rewards")]
    [SerializeField] private float timePenalty = -0.001f;
    [SerializeField] private float damageDealtReward = 0.02f;
    [SerializeField] private float damageTakenPenalty = -0.02f;
    [SerializeField] private float deathPenalty = -1.0f;

    private Rigidbody _rb;
    private Vector3 _spawnPos;
    private Quaternion _spawnRot;

    public override void Initialize()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.freezeRotation = true;

        _spawnPos = transform.position;
        _spawnRot = transform.rotation;

        health.OnDamaged += OnDamaged;
        health.OnDied += OnDied;
    }

    public override void OnEpisodeBegin()
    {
        // Reset
        transform.position = _spawnPos + new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
        transform.rotation = _spawnRot;

        _rb.linearVelocity = Vector3.zero;

        health.ResetHealth();
        if (ammo != null) ammo.ResetAmmo();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        perception.Tick();

        // Self state
        sensor.AddObservation(health.Health01);
        sensor.AddObservation(ammo != null ? ammo.Ammo01 : 1f);

        // Target state (relative)
        if (perception.CurrentTarget != null && perception.HasLineOfSight)
        {
            Vector3 toTarget = perception.CurrentTarget.position - transform.position;
            Vector3 local = transform.InverseTransformDirection(toTarget.normalized);

            sensor.AddObservation(1f); // has target
            sensor.AddObservation(local.x);
            sensor.AddObservation(local.z);
            sensor.AddObservation(Mathf.Clamp01(toTarget.magnitude / 25f));
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(1f);
        }

        // Velocity
        Vector3 vLocal = transform.InverseTransformDirection(_rb.linearVelocity);
        sensor.AddObservation(Mathf.Clamp(vLocal.x / moveSpeed, -1f, 1f));
        sensor.AddObservation(Mathf.Clamp(vLocal.z / moveSpeed, -1f, 1f));
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Continuous: 0=moveX, 1=moveZ, 2=turn
        float moveX = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float moveZ = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        float turn = Mathf.Clamp(actions.ContinuousActions[2], -1f, 1f);

        // Discrete: 0=do nothing, 1=fire
        int fire = actions.DiscreteActions[0];

        // Move
        Vector3 desired = (transform.right * moveX + transform.forward * moveZ) * moveSpeed;
        _rb.linearVelocity = new Vector3(desired.x, _rb.linearVelocity.y, desired.z);

        // Turn
        transform.Rotate(Vector3.up, turn * turnSpeed * Time.deltaTime);

        // Aim & fire (simple)
        if (perception.CurrentTarget != null)
        {
            weapon.AimAt(perception.CurrentTarget.position);
        }

        if (fire == 1 && weapon.CanFire)
            weapon.Fire();

        AddReward(timePenalty);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Optional: test with WASD
        var c = actionsOut.ContinuousActions;
        c[0] = Input.GetAxisRaw("Horizontal");
        c[1] = Input.GetAxisRaw("Vertical");
        c[2] = Input.GetKey(KeyCode.Q) ? -1f : Input.GetKey(KeyCode.E) ? 1f : 0f;

        var d = actionsOut.DiscreteActions;
        d[0] = Input.GetMouseButton(0) ? 1 : 0;
    }

    // Reward for taking damage (penalty)
    private void OnDamaged(float dmg, GameObject source)
    {
        AddReward(damageTakenPenalty * Mathf.Clamp(dmg / 10f, 0.2f, 2f));
    }

    // Penalty on death
    private void OnDied(Health h)
    {
        AddReward(deathPenalty);
        EndEpisode();
    }

    // Call this from arena manager / projectile if you want “damage dealt reward”
    public void RewardDamageDealt(float dmg)
    {
        AddReward(damageDealtReward * Mathf.Clamp(dmg / 10f, 0.2f, 2f));
    }
}
