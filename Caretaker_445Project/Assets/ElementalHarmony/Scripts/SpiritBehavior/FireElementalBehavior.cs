using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FireElementalBehavior : ElementalBehavior
{
    [Header("Fire Elemental Settings")]
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
        // Clean up destroyed pits
        activePits.RemoveAll(pit => pit == null);

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
        // Try spawning a pit if conditions are met
        if (Time.time >= nextPitSpawnTime && activePits.Count < maxActivePits)
        {
            if (TrySpawnFirePit())
            {
                nextPitSpawnTime = Time.time + pitSpawnCooldown;
            }
        }
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

                    // check if on navmesh
                    NavMeshHit navHit;
                    if (NavMesh.SamplePosition(spawnPoint, out navHit, 6, NavMesh.AllAreas))
                    {
                        GameObject pitObj = Instantiate(firePitPrefab, hit.point,
                        Quaternion.Euler(0, Random.Range(0, 360), 0));
                        FirePit pit = pitObj.GetComponent<FirePit>();

                        if (pit != null)
                        {
                            if (actionEffect != null)
                                actionEffect.Play();
                            activePits.Add(pit);
                            Debug.Log($"Fire Spirit spawned new Fire Pit at {hit.point}");
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
        // Fire spirits are more aggressive against Nature spirits
        if (otherSpirit.elementalData.elementalName.Contains("Nature"))
        {
            return stats.currentHP > otherSpirit.currentHP * 0.4f; // More willing to fight Nature Elementals
        }else if (otherSpirit.elementalData.elementalName.Contains("Water"))
        {
            return stats.currentHP > otherSpirit.currentHP * 1.4f; // Less willing to fight Water Elementals
        }

        return base.ShouldFight(otherSpirit);
    }
    public IEnumerator BoostStats(float multiplier, float boostTime)
    {
        // prevent infinite boost
        if (boosted)
            yield return null;
        else
        {
            // boost stats by multiplier
            float defaultHP = stats.currentHP;
            float defaultDamage = stats.damage;

            stats.currentHP *= multiplier;
            stats.damage *= multiplier;
            agent.speed = stats.moveSpeed * multiplier;
            boosted = true;

            yield return new WaitForSeconds(boostTime);
            // return stats to normal
            stats.currentHP = Mathf.Min(stats.currentHP, defaultHP);
            stats.damage = defaultDamage;
            agent.speed = stats.moveSpeed;
            boosted = false;

        }

    }
}
