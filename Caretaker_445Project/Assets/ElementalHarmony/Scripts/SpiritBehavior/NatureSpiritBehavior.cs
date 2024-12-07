using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NatureSpiritBehavior : BaseSpiritBehavior
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
        Debug.Log($"{spiritData.spiritName} is idle");
        agent.isStopped = true;
        stateTimer = idleTime;

        while (stateTimer > 0)
        {
            // Check for threats
            if (CheckForThreats()) yield break;

            // Check for interactables
            if (Random.value < 0.1f && CheckForInteractables()) yield break;
            // Check if it's time to spawn a new plant
            if (Time.time >= nextPlantSpawnTime && activeTrees.Count < maxActiveTrees)
            {
                Debug.Log("Trying To plant");
                if (TrySpawnNewPlant())
                {
                    nextPlantSpawnTime = Time.time + plantSpawnCooldown;
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
}
