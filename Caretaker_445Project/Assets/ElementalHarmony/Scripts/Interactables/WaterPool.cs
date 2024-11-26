using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public enum PoolState
{
    Dry,
    Drying,
    Filling,
    Full
}

public class WaterPool : ElementalObject
{
    [Header("Water Pool Settings")]
    public float healAmount = 20f;
    public float healTickRate = 1f;
    public float growthBoostRadius = 5f;
    public float growthBoostMultiplier = 1.5f;
    private float healTickTimer;
    private List<Plant> boostedPlants = new List<Plant>();
    public PoolState currentState;
    [SerializeField] private GameObject waterVisual;
    public float minScaleSize = 0.2f;
    public float maxScaleSize = 1.5f;
    public float waterChangeTime = 10;
    private float waterChangeProgress = 0f;
    protected override void Update()
    {
        base.Update();
        if (!isActive) return;

        if(currentState == PoolState.Drying)
        {
            DryUpWater();
        }else if (currentState == PoolState.Filling)
        {
            RefillWater();
        }
        else if(currentState == PoolState.Full)
        {
            // Heal spirits in range
            healTickTimer -= Time.deltaTime;
            if (healTickTimer <= 0)
            {
                HealSpiritsInRange();
                healTickTimer = healTickRate;
            }
            spawnTimer += 1f * Time.deltaTime;
            // Boost plant growth
            BoostNearbyPlants();
            if (!hasSpawnedSpirit && spawnTimer >= spiritSpawnTime)
                HandleSpiritSpawning();
        }
    }
    private void HealSpiritsInRange()
    {
        Collider[] nearbySpirits = Physics.OverlapSphere(transform.position, interactionRadius);
        foreach (var collider in nearbySpirits)
        {
            SpiritStats spirit = collider.GetComponent<SpiritStats>();
            

            // only heal water spirits, and if they need healing
            if (spirit != null && spirit.spiritData.spiritName.Contains("Water"))
            {
                if(spirit.HPPercentage() < 1)
                    spirit.Heal(healAmount * Time.deltaTime);
                spawnTimer += 2 * Time.deltaTime;
            }
        }
    }
    private void BoostNearbyPlants()
    {
        Collider[] nearbyPlants = Physics.OverlapSphere(transform.position, growthBoostRadius);
        foreach (var collider in nearbyPlants)
        {
            Plant plant = collider.GetComponent<Plant>();
            if (plant != null && !boostedPlants.Contains(plant))
            {
                // Apply growth boost
                boostedPlants.Add(plant);
                plant.currentGrowthRate *= growthBoostMultiplier;

                spawnTimer += Time.deltaTime;
            }
        }
    }
    public override bool CanInteract(GameObject spirit)
    {
        SpiritStats stats = spirit.GetComponent<SpiritStats>();
        if (stats == null) return false;

        string spiritType = stats.spiritData.spiritName;

        switch (currentState)
        {
            case PoolState.Dry:
                // water spirit can restore pool
                return spiritType.Contains("Water");
            case PoolState.Full:
                // when low on lifetime, a water spirit can restore life time
                if (spiritType.Contains("Water") && currentLifetime / lifetime < .2) return true;
                // nature spirits dry the pool
                return spiritType.Contains("Nature");
        }

        return false;
    }
    protected override IEnumerator InteractInternal(GameObject spirit)
    {
        SpiritStats stats = spirit.GetComponent<SpiritStats>();
        string spiritType = stats.spiritData.spiritName;

        if (spiritType.Contains("Water"))
        {
            yield return HandleWaterInteraction();
        }
        else if (spiritType.Contains("Nature"))
        {
            yield return HandleNatureInteraction();
        }

        yield return null;
    }
    private IEnumerator HandleWaterInteraction()
    {
        // Extend lifetime when water spirit interacts
        currentLifetime = lifetime;
        if(currentState == PoolState.Dry)
            currentState = PoolState.Filling;

        yield return new WaitForSeconds(3f); // Water effect duration
    }
    private IEnumerator HandleNatureInteraction()
    {
        currentState = PoolState.Drying;
        yield return new WaitForSeconds(waterChangeTime); // dry effect duration
    }
    private void RefillWater()
    {
        waterChangeProgress += Time.deltaTime;
        float growthPercent = waterChangeProgress / waterChangeTime;

        // Update scale based on growth
        float currentScale = Mathf.Lerp(minScaleSize, maxScaleSize, growthPercent);
        transform.localScale = Vector3.one * currentScale;

        if (growthPercent >= 1.0f)
        {
            CompleteRefill();
        }
    }
    private void CompleteRefill()
    {
        currentState = PoolState.Full;
        waterChangeProgress = 0;
    }
    private void DryUpWater()
    {
        waterChangeProgress += Time.deltaTime;

        // Scale down as it burns
        float burnPercent = waterChangeProgress / waterChangeTime;
        float burnScale = Mathf.Lerp(maxScaleSize, minScaleSize, burnPercent);
        waterVisual.transform.localScale = Vector3.one * burnScale;

        if (waterChangeProgress >= waterChangeTime)
        {
            DryComplete();
            hasSpawnedSpirit = false; // Reset spirit spawn status when burned
            spawnTimer = 0;
        }
    }
    private void DryComplete()
    {
        currentState = PoolState.Dry;
        waterChangeProgress = 0;
    }
    protected override IEnumerator DespawnRoutine()
    {
        // Remove growth boosts before despawning
        foreach (var plant in boostedPlants)
        {
            if (plant != null)
            {
                plant.currentGrowthRate /= growthBoostMultiplier;
            }
        }

        yield return base.DespawnRoutine();
    }
    private void HandleSpiritSpawning()
    {

        if (Random.value <= spiritSpawnChance)
        {
            StartCoroutine(SpawnElemental());
        }
        hasSpawnedSpirit = true; // Prevent future spawn attempts even if this one failed
        
    }
    protected override IEnumerator SpawnElemental()
    {
        if (spiritPrefab == null)
        {
            Debug.LogWarning("Nature Spirit Prefab not assigned to PlantGrowthSystem!");
            yield break;
        }

        // Start spawning effect
        if (spiritSpawnVFX != null)
        {
            spiritSpawnVFX.Play();
        }

        // Wait for effect to build up
        yield return new WaitForSeconds(2f);

        // Calculate spawn position above the plant
        Vector3 spawnPosition = transform.position + Vector3.up * spiritSpawnHeight;

        // Create spirit with a gentle floating animation
        GameObject spiritObj = Instantiate(spiritPrefab, spawnPosition, Quaternion.identity);
        PlayerManager.Instance.UpdateElementalSpiritCount(spiritObj.GetComponent<SpiritStats>());
        // Optionally animate the spirit's entrance
        StartCoroutine(AnimateSpiritSpawn(spiritObj));

        Debug.Log("Nature Spirit spawned from mature plant!");
    }
    private IEnumerator AnimateSpiritSpawn(GameObject spirit)
    {
        if (spirit == null) yield break;

        // Store initial position and set starting scale
        Vector3 startPos = spirit.transform.position;
        spirit.transform.localScale = Vector3.zero;

        // Animate scale and position over 1 second
        float elapsedTime = 0f;
        float animationDuration = 1f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / animationDuration;

            // Smooth step for more pleasant animation
            float smoothT = t * t * (3f - 2f * t);

            // Scale up from 0
            spirit.transform.localScale = Vector3.one * smoothT;

            // Optional gentle float upward
            spirit.transform.position = startPos + Vector3.up * (smoothT * 0.5f);

            yield return null;
        }

        // Ensure final scale and position are exact
        spirit.transform.localScale = Vector3.one;
        spirit.transform.position = startPos + Vector3.up * 0.5f;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, growthBoostRadius);
    }
}
