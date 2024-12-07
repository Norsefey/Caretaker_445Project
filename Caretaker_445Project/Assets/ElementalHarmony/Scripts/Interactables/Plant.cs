using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum PlantState
{
    Seedling,
    Growing,
    Mature,
    Burning,
    Burned
}
public class Plant : ElementalObject
{
    [Header("Growth Settings")]
    public float baseGrowthTime = 30f;
    public float waterGrowthBoost = 2f;
    public float minScaleSize = 0.2f;
    public float maxScaleSize = 1.5f;

    [Header("Burning Settings")]
    public float burnTime = 10f;
    public float fireSpreadRadius = 3f;
    public LayerMask plantLayer;

    [Header("Visual Elements")]
    public GameObject seedlingModel;
    public GameObject growingModel;
    public GameObject matureModel;
    public GameObject burnedModel;
    public ParticleSystem wateringVFX;
    public ParticleSystem fireVFX;
    public ParticleSystem growthVFX;

    public PlantState currentState = PlantState.Seedling;
    private float growthProgress = 0f;
    public float currentGrowthRate = 1f;
    private bool isBeingWatered = false;
    private bool isWatered = false;
    private bool isOnFire = false;
    private float fireProgress = 0f;
    private float timeSpentMature = 0f;
    protected override void Start()
    {
        base.Start();
        UpdateVisuals();
        StartCoroutine(GrowthRoutine());
    }

    protected override void Update()
    {
        base.Update();


        if (isOnFire && !isBeingWatered)
        {
            HandleBurning();
        }
        else if (currentState == PlantState.Mature && !hasSpawnedElemental)
        {
            HandleElementalSpawning();
        }
    }

    public override bool CanInteract(ElementalBehavior elemental)
    {
        ElementalStats stats = elemental.stats;
        if (stats == null) return false;

        string elementalType = stats.elementalData.elementalName;

        switch (currentState)
        {
            case PlantState.Seedling:// does not have a break to allow for interaction at seedling/grow/mature state
            case PlantState.Growing:
            case PlantState.Mature:
                // Water elementals can water at any stage before burning
                if (elementalType.Contains("Water")) return !isWatered;
                // Fire elementals can set it on fire if it's not already burning
                else if (elementalType.Contains("Fire")) return !isOnFire;
                else if (elementalType.Contains("Nature") && currentLifetime / lifetime < .2) return true;
                break;

            case PlantState.Burning:
                // Only water elementals can interact with burning plants
                return elementalType.Contains("Water");

            case PlantState.Burned:
                // Nature elementals can clear burned plants and plant new ones
                return elementalType.Contains("Nature");
        }

        return false;
    }
    protected override IEnumerator InteractInternal(ElementalBehavior elemental)
    {
        ElementalStats stats = elemental.stats;
        string elementalType = stats.elementalData.elementalName;

        if (elementalType.Contains("Water"))
        {
            yield return HandleWaterInteraction();
        }
        else if (elementalType.Contains("Fire"))
        {
            yield return HandleFireInteraction();
        }
        else if (elementalType.Contains("Nature"))
        {
            yield return HandleNatureInteraction();
        }
    }
    private IEnumerator GrowthRoutine()
    {
        while (currentState != PlantState.Mature && currentState != PlantState.Burned)
        {
            if (currentState == PlantState.Burning) yield break;

            growthProgress += Time.deltaTime * currentGrowthRate;
            float growthPercent = growthProgress / baseGrowthTime;
            // Update scale based on growth
            float currentScale = Mathf.Lerp(minScaleSize, maxScaleSize, growthPercent);
            transform.localScale = Vector3.one * currentScale;

            // Update state based on growth
            if (growthPercent >= 0.3f && currentState == PlantState.Seedling)
            {
                currentState = PlantState.Growing;
                UpdateVisuals();
            }
            else if (growthPercent >= 1.0f)
            {
                currentState = PlantState.Mature;
                UpdateVisuals();
                timeSpentMature = 0f; // Start tracking mature time
            }

            yield return null;
        }
    }
    private void HandleElementalSpawning()
    {
        timeSpentMature += Time.deltaTime;

        if (timeSpentMature >= elementalSpawnTime)
        {
            if (Random.value <= elementalSpawnChance)
            {
                StartCoroutine(SpawnElemental());
            }
            hasSpawnedElemental = true; // Prevent future spawn attempts even if this one failed
        }
    }
    protected override IEnumerator SpawnElemental()
    {
        if (elementalPrefab == null)
        {
            Debug.LogWarning("Nature Elemental Prefab not assigned to PlantGrowthSystem!");
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

        // Create Elemental with a gentle floating animation
        GameObject elementalObj = Instantiate(elementalPrefab, spawnPosition, Quaternion.identity);
        // Optionally animate the Elemental's entrance
        StartCoroutine(AnimateSpawn(elementalObj));

        Debug.Log("Nature Elemental spawned from mature plant!");
    }
    private IEnumerator AnimateSpawn(GameObject Elemental)
    {
        if (Elemental == null) yield break;

        // Store initial position and set starting scale
        Vector3 startPos = Elemental.transform.position;
        Elemental.transform.localScale = Vector3.zero;

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
            Elemental.transform.localScale = Vector3.one * smoothT;

            // Optional gentle float upward
            Elemental.transform.position = startPos + Vector3.up * (smoothT * 0.5f);

            yield return null;
        }

        // Ensure final scale and position are exact
        Elemental.transform.localScale = Vector3.one;
        Elemental.transform.position = startPos + Vector3.up * 0.5f;
    }
    private IEnumerator HandleWaterInteraction()
    {
        Debug.Log("Water Elemental watering the plant");
        isBeingWatered = true;

        if (wateringVFX != null)
            wateringVFX.Play();

        if (isOnFire)
        {
            // Extinguish fire
            yield return ExtinguishFire();
        }
        else
        {
            // Boost growth
            float originalGrowthRate = currentGrowthRate;
            currentGrowthRate = waterGrowthBoost;
            yield return new WaitForSeconds(5f); // Water effect duration
            currentGrowthRate = originalGrowthRate;
        }

        if (wateringVFX != null)
            wateringVFX.Stop();

        isWatered = true;
        isBeingWatered = false;
    }
    public IEnumerator HandleFireInteraction()
    {
        // if already on fire return, or already burned
        if (isOnFire || currentState == PlantState.Burned) yield break;

        Debug.Log("Tree has been ignited");
        isOnFire = true;
        currentState = PlantState.Burning;

        if (fireVFX != null)
            fireVFX.Play();

        // Start fire spread coroutine
        StartCoroutine(SpreadFire());

        yield return null;
    }
    private IEnumerator HandleNatureInteraction()
    {
        currentLifetime = lifetime;

        if (currentState != PlantState.Burned) yield break;

        Debug.Log("Nature Elemental clearing burned plant");

        // Clear the burned plant
        currentState = PlantState.Seedling;
        growthProgress = 0f;
        fireProgress = 0f;
        isOnFire = false;
        hasSpawnedElemental = false; // Reset spirit spawn status for new growth cycle
        timeSpentMature = 0f;
        UpdateVisuals();

        if (growthVFX != null)
            growthVFX.Play();

        yield return new WaitForSeconds(2f);

        if (growthVFX != null)
            growthVFX.Stop();
    }
    private void HandleBurning()
    {
        fireProgress += Time.deltaTime;

        // Scale down as it burns
        float burnPercent = fireProgress / burnTime;
        float burnScale = Mathf.Lerp(maxScaleSize, minScaleSize, burnPercent);
        transform.localScale = Vector3.one * burnScale;

        if (fireProgress >= burnTime)
        {
            BurnComplete();
            hasSpawnedElemental = false; // Reset Elemental spawn status when burned
            timeSpentMature = 0f;
        }
    }
    private IEnumerator ExtinguishFire()
    {
        Debug.Log("Extinguishing fire");

        // Gradually reduce fire effect
        float extinguishTime = 2f;
        float startFireProgress = fireProgress;

        float elapsed = 0f;
        while (elapsed < extinguishTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / extinguishTime;

            // Reduce fire effect
            if (fireVFX != null)
            {
                var emission = fireVFX.emission;
                emission.rateOverTime = Mathf.Lerp(emission.rateOverTime.constant, 0f, t);
            }

            yield return null;
        }

        isOnFire = false;
        fireVFX.Stop();

        // If the plant wasn't completely burned, return to growing state
        if (fireProgress < burnTime * 0.7f)
        {
            currentState = growthProgress >= baseGrowthTime ? PlantState.Mature : PlantState.Growing;
            UpdateVisuals();
            StartCoroutine(GrowthRoutine());
        }
        else
        {
            BurnComplete();
        }
    }
    private void BurnComplete()
    {
        isOnFire = false;
        currentState = PlantState.Burned;
        if (fireVFX != null)
            fireVFX.Stop();
        UpdateVisuals();
    }
    private IEnumerator SpreadFire()
    {
        while (isOnFire)
        {
            // Check for nearby plants
            Collider[] nearbyPlants = Physics.OverlapSphere(transform.position, fireSpreadRadius, plantLayer);
            foreach (var collider in nearbyPlants)
            {
                if (collider.gameObject == gameObject) continue;

                Plant nearbyPlant = collider.GetComponent<Plant>();
                if (nearbyPlant != null && !nearbyPlant.isOnFire && nearbyPlant.currentState != PlantState.Burned)
                {
                    // Random chance to spread fire
                    if (Random.value < 0.3f)
                    {
                        StartCoroutine(nearbyPlant.HandleFireInteraction());
                    }
                }
            }

            yield return new WaitForSeconds(2f);
        }
    }
    private void UpdateVisuals()
    {
        // Disable all models first
        seedlingModel.SetActive(false);
        growingModel.SetActive(false);
        matureModel.SetActive(false);
        burnedModel.SetActive(false);

        // Enable the appropriate model
        switch (currentState)
        {
            case PlantState.Seedling:
                seedlingModel.SetActive(true);
                break;
            case PlantState.Growing:
                if(isWatered) isWatered = false;
                growingModel.SetActive(true);
                break;
            case PlantState.Mature:
                if (isWatered) isWatered = false;
                matureModel.SetActive(true);
                break;
            case PlantState.Burned:
                if (isWatered) isWatered = false;
                burnedModel.SetActive(true);
                break;
        }
    }
    private void OnDrawGizmosSelected()
    {
        // Draw fire spread radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, fireSpreadRadius);
    }
}
