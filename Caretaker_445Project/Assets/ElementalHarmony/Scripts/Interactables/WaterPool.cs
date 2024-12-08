using System.Collections;
using System.Collections.Generic;
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
    [Header("Water Visual")]
    [SerializeField] private GameObject waterVisual;
    public float minScaleSize = 0.2f;
    public float maxScaleSize = 1.5f;
    public float waterChangeTime = 10;
    private float waterChangeProgress = 0f;
    protected override void Update()
    {
        base.Update();
        if(currentState == PoolState.Drying)
        {
            DryUpWater();
        }else if (currentState == PoolState.Filling)
        {
            RefillWater();
        }
        else if(currentState == PoolState.Full)
        {
            // Heal Elementals in range
            healTickTimer -= Time.deltaTime;
            if (healTickTimer <= 0)
            {
                HealElementalsInRange();
                healTickTimer = healTickRate;
            }
            spawnTimer += 1f * Time.deltaTime;
            // Boost plant growth
            BoostNearbyPlants();
            if (!hasSpawnedElemental && spawnTimer >= elementalSpawnTime)
                HandlElementalSpawning();
        }
    }
    private void HealElementalsInRange()
    {
        Collider[] nearbyElemental = Physics.OverlapSphere(transform.position, interactionRadius);
        foreach (var collider in nearbyElemental)
        {
            ElementalStats Elemental = collider.GetComponent<ElementalStats>();
            

            // only heal water spirits, and if they need healing
            if (Elemental != null && Elemental.elementalData.elementalName.Contains("Water"))
            {
                if(Elemental.HPPercentage() < 1)
                    Elemental.Heal(healAmount * Time.deltaTime);
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
    public override bool CanInteract(ElementalBehavior Elemental)
    {
        ElementalStats stats = Elemental.stats;
        if (stats == null) return false;

        string ElementalType = stats.elementalData.elementalName;

        switch (currentState)
        {
            case PoolState.Dry:
                // water Elemental can restore pool
                return ElementalType.Contains("Water");
            case PoolState.Full:
                // when low on lifetime, a water Elemental can restore life time
                if (ElementalType.Contains("Water") && currentLifetime / lifetime < .2) return true;
                // nature Elemental dry the pool
                return ElementalType.Contains("Nature");
        }

        return false;
    }
    protected override IEnumerator InteractInternal(ElementalBehavior Elemental)
    {
        ElementalStats stats = Elemental.GetComponent<ElementalStats>();
        string ElementalType = stats.elementalData.elementalName;

        if (ElementalType.Contains("Water"))
        {
            yield return HandleWaterInteraction();
        }
        else if (ElementalType.Contains("Nature"))
        {
            yield return HandleNatureInteraction();
        }

        yield return null;
    }
    private IEnumerator HandleWaterInteraction()
    {
        // Extend lifetime when water Elemental interacts
        currentLifetime = lifetime;
        if(currentState == PoolState.Dry)
            currentState = PoolState.Filling;

        yield return new WaitForSeconds(2f); // Water effect duration
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
            hasSpawnedElemental = false; // Reset Elemental spawn status when burned
            spawnTimer = 0;
        }
    }
    private void DryComplete()
    {
        currentState = PoolState.Dry;
        waterChangeProgress = 0;
    }
    protected override void Despawn()
    {
        // Remove growth boosts before despawning
        foreach (var plant in boostedPlants)
        {
            if (plant != null)
            {
                plant.currentGrowthRate /= growthBoostMultiplier;
            }
        }

       base.Despawn();
    }
    private void HandlElementalSpawning()
    {

        if (Random.value <= elementalSpawnChance)
        {
            StartCoroutine(SpawnElemental());
        }
        hasSpawnedElemental = true; // Prevent future spawn attempts even if this one failed
        
    }
    protected override IEnumerator SpawnElemental()
    {
        if (elementalPrefab == null)
        {
            Debug.LogWarning("Water Elemental Prefab not assigned to PlantGrowthSystem!");
            yield break;
        }

        // Start spawning effect
        if (elementalSpawnVFX != null)
        {
            elementalSpawnVFX.Play();
        }

        // Wait for effect to build up
        yield return new WaitForSeconds(2f);

        // Calculate spawn position above the plant
        Vector3 spawnPosition = transform.position + Vector3.up * elementalSpawnHeight;

        // Create Elemental 
        GameObject spiritObj = Instantiate(elementalPrefab, spawnPosition, Quaternion.identity);
        // animate the spirit's entrance with a gentle floating animation
        StartCoroutine(AnimateSpawn(spiritObj));

        Debug.Log("Nature Spirit spawned from mature plant!");
    }
    private IEnumerator AnimateSpawn(GameObject spirit)
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

            // gentle float upward
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
