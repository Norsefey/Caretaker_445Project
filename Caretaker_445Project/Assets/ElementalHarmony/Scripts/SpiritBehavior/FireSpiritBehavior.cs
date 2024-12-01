using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class FireSpiritBehavior : BaseSpiritBehavior
{
    [Header("Fire Spirit Settings")]
    public float igniteRange = 6f;
    public float igniteDuration = 2f;

    [Header("Fire Pit Settings")]
    public GameObject firePitPrefab;
    public float pitSpawnCooldown = 60f;
    public LayerMask obstacleLayer;
    public LayerMask groundLayer;
    public int maxActivePits = 2;
    public float pitSpawnRadius = 5f;
    public int maxPitsPerArea = 5;
    public float pitDensityCheckRadius = 10f;
    public bool boosted = false;
    private float nextPitSpawnTime;
    private List<FirePit> activePits = new List<FirePit>();

    protected override IEnumerator HandleIdleState()
    {
        Debug.Log($"{spiritData.spiritName} is idle");
        agent.isStopped = true;
        stateTimer = idleTime;

        while (stateTimer > 0)
        {
            // Check for threats
            if (CheckForThreats()) yield break;

            // Check for interactables
            if (Random.value < 0.1f && CheckForInteractables()) yield break;

            // Clean up destroyed pits
            activePits.RemoveAll(pit => pit == null);

            // Try spawning a pit if conditions are met
            if (Time.time >= nextPitSpawnTime && activePits.Count < maxActivePits)
            {
                if (TrySpawnFirePit())
                {
                    nextPitSpawnTime = Time.time + pitSpawnCooldown;
                }
            }
            stateTimer -= Time.deltaTime;
            yield return null;
        }

        // Transition to next state
        if (Random.value < sleepChance)
            TransitionToState(SpiritState.Sleeping);
        else
            TransitionToState(SpiritState.Roaming);
    }
    private bool TrySpawnFirePit()
    {
        // Check if there are too many plants nearby
        Collider[] nearbyPlants = Physics.OverlapSphere(transform.position, pitDensityCheckRadius,
            LayerMask.GetMask("Interactable"));

        if (nearbyPlants.Length >= maxPitsPerArea)
        {
            stats.DecreaseHappiness(1);
            return false;

        }
        // Try several positions
        for (int i = 0; i < 5; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * pitSpawnRadius;
            Vector3 spawnPoint = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

            RaycastHit hit;
            if (Physics.Raycast(spawnPoint + Vector3.up * 5f, Vector3.down, out hit, 10f, groundLayer))
            {
                // Check for obstacles
                if (!Physics.CheckSphere(hit.point, 1f, obstacleLayer))
                {
                    GameObject pitObj = Instantiate(firePitPrefab, hit.point,
                        Quaternion.Euler(0, Random.Range(0, 360), 0));
                    FirePit pit = pitObj.GetComponent<FirePit>();

                    if (pit != null)
                    {
                        activePits.Add(pit);
                        Debug.Log($"Fire Spirit spawned new Fire Pit at {hit.point}");
                        stats.IncreaseHappiness(10);
                        return true;
                    }

                }
            }
        }
        stats.DecreaseHappiness(1);
        return false;
    }
    protected override bool ShouldFight(SpiritStats otherSpirit)
    {
        // Fire spirits are more aggressive against Nature spirits
        if (otherSpirit.spiritData.spiritName.Contains("Nature"))
        {
            return stats.currentHP > otherSpirit.currentHP * 0.8f; // More willing to fight Nature spirits
        }

        return base.ShouldFight(otherSpirit);
    }
    public IEnumerator BoostStats(float multiplier, float boostTime)
    {
        if (boosted)
            yield return null;
        else
        {
            float defaultHP = stats.currentHP;
            float defaultDamage = stats.damage;

            stats.currentHP *= multiplier;
            stats.damage *= multiplier;
            agent.speed = stats.moveSpeed * multiplier;
            boosted = true;
            yield return new WaitForSeconds(boostTime);
            stats.currentHP = Mathf.Min(stats.currentHP, defaultHP);
            stats.damage = defaultDamage;
            agent.speed = stats.moveSpeed;
            boosted = false;

        }

    }
    // Fire spirits avoid water spirits at lower health
    protected override IEnumerator HandleFleeState()
    {
        agent.speed = stats.moveSpeed * 1.2f; // Faster flee speed for fire spirits
        yield return base.HandleFleeState();
    }
}
