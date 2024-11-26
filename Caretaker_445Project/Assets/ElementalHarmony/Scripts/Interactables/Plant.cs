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
        else if (currentState == PlantState.Mature && !hasSpawnedSpirit)
        {
            HandleSpiritSpawning();
        }
    }

    public override bool CanInteract(GameObject spirit)
    {
        SpiritStats stats = spirit.GetComponent<SpiritStats>();
        if (stats == null) return false;

        string spiritType = stats.spiritData.spiritName;

        switch (currentState)
        {
            case PlantState.Seedling:
            case PlantState.Growing:
            case PlantState.Mature:
                // Water spirits can water at any stage before burning
                if (spiritType.Contains("Water")) return !isWatered;
                // Fire spirits can set it on fire if it's not already burning
                else if (spiritType.Contains("Fire")) return !isOnFire;
                else if (spiritType.Contains("Nature") && currentLifetime / lifetime < .2) return true;
                break;

            case PlantState.Burning:
                // Only water spirits can interact with burning plants
                return spiritType.Contains("Water");

            case PlantState.Burned:
                // Nature spirits can clear burned plants and plant new ones
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
        else if (spiritType.Contains("Fire"))
        {
            yield return HandleFireInteraction();
        }
        else if (spiritType.Contains("Nature"))
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
    private void HandleSpiritSpawning()
    {
        timeSpentMature += Time.deltaTime;

        if (timeSpentMature >= spiritSpawnTime)
        {
            if (Random.value <= spiritSpawnChance)
            {
                StartCoroutine(SpawnElemental());
            }
            hasSpawnedSpirit = true; // Prevent future spawn attempts even if this one failed
        }
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
    private IEnumerator HandleWaterInteraction()
    {
        Debug.Log("Water spirit watering the plant");
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
        if (isOnFire) yield break;

        Debug.Log("Fire spirit igniting the plant");
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

        Debug.Log("Nature spirit clearing burned plant");

        // Clear the burned plant
        currentState = PlantState.Seedling;
        growthProgress = 0f;
        fireProgress = 0f;
        isOnFire = false;
        hasSpawnedSpirit = false; // Reset spirit spawn status for new growth cycle
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
            hasSpawnedSpirit = false; // Reset spirit spawn status when burned
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
