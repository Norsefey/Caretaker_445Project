using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
public enum FirePitState
{// can be active and deactivated
    lit,
    unlit
}
public class FirePit : ElementalObject
{
    [Header("Fire Pit Settings")]
    public float spreadRadius = 15f;// can set nearby trees on fire
    [Space(5)]
    public GameObject emberPrefab;// spawns an ember that sets trees on fire
    [Space(5)]
    public float actionInterval = 5f;// how often it should do its action of seting plants on fire and spawning embers
    private float actionTimer;

    private FirePitState currentState;

    [SerializeField] private GameObject fireVisual;

    protected override void Update()
    {
        // base update checks counts down lifetime
        base.Update();
        // only spawns embers and lights fires if lit
        if(currentState == FirePitState.lit)
        {
            // Spawn fire trails and boost fire elementals
            actionTimer -= Time.deltaTime;
            if (actionTimer <= 0)
            {
                AffectElementalsInRange();
                SpawnEmber();
                actionTimer = actionInterval;
            }

            spawnTimer += 1f * Time.deltaTime;
            if(spawnTimer > elementalSpawnTime && !hasSpawnedElemental)
            {
                HandleSpiritSpawning();
            }
            // Check for plants to ignite
            IgniteNearbyPlants();
        }
    }
    private void AffectElementalsInRange()
    {
        Collider[] nearbySpirits = Physics.OverlapSphere(transform.position, interactionRadius);
        foreach (var collider in nearbySpirits)
        {
            ElementalStats spirit = collider.GetComponent<ElementalStats>();
            if (spirit != null && spirit.elementalData.elementalName.Contains("Fire"))
            {
                FireElementalBehavior spiritBehavior = spirit.GetComponent<FireElementalBehavior>();
                StartCoroutine(spiritBehavior.BoostStats(2, 5));
                spawnTimer += 2 * Time.deltaTime;
            }
        }
    }
    private void HandleSpiritSpawning()
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

        // Create spirit with a gentle floating animation
        GameObject elementalObj = Instantiate(elementalPrefab, spawnPosition, Quaternion.identity);
        
        // animate the spirit's entrance
        StartCoroutine(AnimateSpawn(elementalObj));

    }
    private IEnumerator AnimateSpawn(GameObject elemental)
    {
        if (elemental == null) yield break;

        // Store initial position and set starting scale
        Vector3 startPos = elemental.transform.position;
        elemental.transform.localScale = Vector3.zero;

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
            elemental.transform.localScale = Vector3.one * smoothT;

            // Optional gentle float upward
            elemental.transform.position = startPos + Vector3.up * (smoothT * 0.5f);

            yield return null;
        }

        // Ensure final scale and position are exact
        elemental.transform.localScale = Vector3.one;
        elemental.transform.position = startPos + Vector3.up * 0.5f;
    }
    private void SpawnEmber()
    {
        if (emberPrefab == null) return;

        // Spawn in random direction
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        Vector3 spawnPos = transform.position + new Vector3(randomDir.x, 0, randomDir.y) * spreadRadius;

        Instantiate(emberPrefab, transform.position, Quaternion.identity);
    }
    private void IgniteNearbyPlants()
    {
        Collider[] nearbyPlants = Physics.OverlapSphere(transform.position, spreadRadius);
        foreach (var collider in nearbyPlants)
        {
            Plant plant = collider.GetComponent<Plant>();
            if (plant != null && Random.value < 0.1f * Time.deltaTime)
            {
                StartCoroutine(plant.HandleFireInteraction());
                spawnTimer += 5;
            }
        }
    }
    public override bool CanInteract(ElementalBehavior elemental)
    {
        ElementalStats stats = elemental.stats;
        if (stats == null || beingInteracted) return false;

        string elementalType = stats.elementalData.elementalName;

        switch (currentState)
        {
            case FirePitState.unlit:
                // Fire spirit can relight pit
                return elementalType.Contains("Fire");
            case FirePitState.lit:
                // when low on lifetime, a Fire elemental can restore life time, less then 20% otherwise elementals dont wander enough
                if (elementalType.Contains("Fire") && currentLifetime / lifetime < .2) return true;
                // Water spirits puts out pit
                return elementalType.Contains("Water");
        }

        return false;
    }
    private IEnumerator HandleFireInteraction()
    {
        currentLifetime = lifetime;
        if (currentState == FirePitState.unlit)
            currentState = FirePitState.lit;
        yield return new WaitForSeconds(3f);

        beingInteracted = false;
        fireVisual.SetActive(true);

    }
    private IEnumerator HandleWaterInteraction()
    {
        if (currentState == FirePitState.lit)
            currentState = FirePitState.unlit;

        yield return new WaitForSeconds(3f); // Water effect duration
        beingInteracted = false;

        // Hid fire visual
        fireVisual.SetActive(false);
    }
    protected override IEnumerator InteractInternal(ElementalBehavior elemental)
    {
        beingInteracted = true;

        ElementalStats stats = elemental.GetComponent<ElementalStats>();
        string elementalType = stats.elementalData.elementalName;

        if (elementalType.Contains("Fire"))
        {
            yield return HandleFireInteraction();
        }
        else if (elementalType.Contains("Water"))
        {
            yield return HandleWaterInteraction();
        }

        yield return null;
        beingInteracted = false;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spreadRadius);
    }
}
