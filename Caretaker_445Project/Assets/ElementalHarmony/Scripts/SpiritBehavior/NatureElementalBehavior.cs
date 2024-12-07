using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NatureElementalBehavior : ElementalBehavior
{
    [Header("Plant Settings")]
    public GameObject plantPrefab;
    public float plantSpawnRadius = 5f;
    public float plantSpawnCooldown = 30f;
    public LayerMask groundLayer;
    public LayerMask obstacleLayer;
    public int maxPlantsPerArea = 5;
    public float plantDensityCheckRadius = 10f;
    public int maxActiveTrees = 3;
    private List<Plant> activeTrees = new List<Plant>();

    private float nextPlantSpawnTime;

    protected override IEnumerator HandleIdleState()
    {
        activeTrees.RemoveAll(pit => pit == null);

        Debug.Log($"{elementalData.elementalName} is idle");
        agent.isStopped = true;
        stateTimer = idleTime;

        while (stateTimer > 0)
        {
            // Check for threats
            if (CheckForThreats()) yield break;

            // Check for interactables
            if (Random.value < 0.1f && CheckForInteractables()) yield break;

            TrySpawnObject();

            stateTimer -= Time.deltaTime;
            stats.RestoreStamina(.5f);

            yield return null;
        }

        // If low on stamina or HP, go to sleep to recover, with some random chance if not low on anything
        if (stats.HPPercentage() < .25f || stats.currentStamina < stats.maxStamina / 2 || Random.value < sleepChance)
            TransitionToState(ElementalState.Sleeping);
        else// from Idle go explore
            TransitionToState(ElementalState.Roaming);

    }
    protected override void TrySpawnObject()
    {
        // Check if it's time to spawn a new plant
        if (Time.time >= nextPlantSpawnTime && activeTrees.Count < maxActiveTrees)
        {
            Debug.Log("Trying To plant");
            if (TrySpawnNewPlant())
            {
                nextPlantSpawnTime = Time.time + plantSpawnCooldown;
            }
        }
    }
    private bool TrySpawnNewPlant()
    {
        // Check if there are too many plants nearby
        Collider[] nearbyPlants = Physics.OverlapSphere(transform.position, plantDensityCheckRadius,
            LayerMask.GetMask("Interactable"));

        if (nearbyPlants.Length >= maxPlantsPerArea)
        {
            stats.DecreaseHappiness(1);
            return false;

        }
        // Try several positions
        for (int i = 0; i < 5; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * plantSpawnRadius;
            Vector3 spawnPoint = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

            RaycastHit hit;
            if (Physics.Raycast(spawnPoint + Vector3.up * 5f, Vector3.down, out hit, 10f, groundLayer))
            {
                // Check for obstacles
                if (!Physics.CheckSphere(hit.point, 1f, obstacleLayer))
                {
                    // check if on navmesh
                    NavMeshHit navHit;
                    if (NavMesh.SamplePosition(spawnPoint, out navHit, 6, NavMesh.AllAreas))
                    {
                        GameObject newPlant = Instantiate(plantPrefab, hit.point,
                        Quaternion.Euler(0, Random.Range(0, 360), 0));
                        Plant tree = newPlant.GetComponent<Plant>();
                        if(tree != null)
                        {
                            if(actionEffect != null)
                                actionEffect.Play();
                            activeTrees.Add(tree);
                            Debug.Log($"Nature Spirit spawned new plant at {hit.point}");
                            stats.IncreaseHappiness(10);
                            return true;
                        }
                    }
                }
            }
        }
        stats.DecreaseHappiness(1);
        return false;
    }
    protected override bool ShouldFight(ElementalStats otherSpirit)
    {
        // Water spirits are more aggressive against Fire spirits
        if (otherSpirit.elementalData.elementalName.Contains("Water"))
        {
            return stats.currentHP > otherSpirit.currentHP * 0.4f; // More willing to fight Water spirits
        }
        else if (otherSpirit.elementalData.elementalName.Contains("Fire"))
        {
            return stats.currentHP > otherSpirit.currentHP * 1.4f; // Less willing to fight Fire spirits
        }

        return base.ShouldFight(otherSpirit);
    }

}
