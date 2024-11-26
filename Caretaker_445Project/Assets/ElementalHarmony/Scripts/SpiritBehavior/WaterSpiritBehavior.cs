using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterSpiritBehavior : BaseSpiritBehavior
{
    [Header("Water Spirit Settings")]
    public float wateringRange = 8f;
    public ParticleSystem wateringEffect;
    public float wateringDuration = 3f;

    [Header("Water Pool Settings")]
    public GameObject poolPrefab;
    public float poolSpawnCooldown = 45f;
    public LayerMask obstacleLayer;
    public LayerMask groundLayer;
    public int maxActivePools = 3;
    public float poolSpawnRadius = 5f;
    public int maxPoolsPerArea = 5;
    public float poolDensityCheckRadius = 10f;
    private float nextPoolSpawnTime;
    private List<WaterPool> activePools = new List<WaterPool>();

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

            // Clean up destroyed pools
            activePools.RemoveAll(pool => pool == null);

            // Try spawning a pool if conditions are met
            if (Time.time >= nextPoolSpawnTime && activePools.Count < maxActivePools)
            {
                if (TrySpawnWaterPool())
                {
                    nextPoolSpawnTime = Time.time + poolSpawnCooldown;
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
    private bool TrySpawnWaterPool()
    {
        // Check if there are too many plants nearby
        Collider[] nearbyPools = Physics.OverlapSphere(transform.position, poolDensityCheckRadius,
            LayerMask.GetMask("Interactable"));

        if (nearbyPools.Length >= maxPoolsPerArea)
        {
            stats.DecreaseHappiness(1);
            return false;

        }
        // Try several positions
        for (int i = 0; i < 5; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * poolSpawnRadius;
            Vector3 spawnPoint = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

            RaycastHit hit;
            if (Physics.Raycast(spawnPoint + Vector3.up * 5f, Vector3.down, out hit, 10f, groundLayer))
            {
                // Check for obstacles
                if (!Physics.CheckSphere(hit.point, 1f, obstacleLayer))
                {
                    GameObject poolObj = Instantiate(poolPrefab, hit.point,
                        Quaternion.Euler(0, Random.Range(0, 360), 0));
                    WaterPool pool = poolObj.GetComponent<WaterPool>();
                    if (pool != null)
                    {
                        activePools.Add(pool);
                        Debug.Log($"Water Spirit spawned new Pool at {hit.point}");
                        stats.IncreaseHappiness(10);
                        return true;
                    }
            
                }
            }
        }
        stats.DecreaseHappiness(1);
        return false;
    }
    protected override IEnumerator HandleSpecializedInteraction(IInteractable interactable)
    {
        if (interactable is Plant plant)
        {
            // Play watering effect
            if (wateringEffect != null)
            {
                wateringEffect.Play();
                yield return new WaitForSeconds(wateringDuration);
                wateringEffect.Stop();
            }
            stats.IncreaseHappiness(20);
        }

        yield return base.HandleSpecializedInteraction(interactable);
    }
    protected override bool ShouldFight(SpiritStats otherSpirit)
    {
        // Water spirits are more aggressive against Fire spirits
        if (otherSpirit.spiritData.spiritName.Contains("Fire"))
        {
            return stats.currentHP > otherSpirit.currentHP * 0.8f; // More willing to fight Fire spirits
        }

        return base.ShouldFight(otherSpirit);
    }
    protected override bool CheckForInteractables()
    {
        // Prioritize burning plants
        Collider[] nearbyInteractables = Physics.OverlapSphere(transform.position, wateringRange,
            interactableLayer);

        foreach (var collider in nearbyInteractables)
        {
            Plant plant = collider.GetComponent<Plant>();
            if (plant != null && plant.currentState == PlantState.Burning)
            {
                currentInteractable = plant;
                TransitionToState(SpiritState.Interacting);
                return true;
            }
        }

        return base.CheckForInteractables();
    }
}
