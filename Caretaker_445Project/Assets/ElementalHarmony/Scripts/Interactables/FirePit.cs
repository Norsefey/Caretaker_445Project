using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
public enum FirePitState
{
    lit,
    unlit
}
public class FirePit : ElementalObject
{
    [Header("Fire Pit Settings")]
    public float damageAmount = 15f;
    public float damageTickRate = 1f;
    public float spreadRadius = 4f;
    public GameObject fireTrailPrefab;
    public float trailSpawnInterval = 5f;

    private float damageTickTimer;
    private float trailSpawnTimer;
    private FirePitState currentState;

    [SerializeField] private GameObject fireVisual;

    protected override void Update()
    {
        base.Update();
        if (!isActive) return;

        if(currentState == FirePitState.lit)
        {
            // Damage non-fire spirits in range
            damageTickTimer -= Time.deltaTime;
            if (damageTickTimer <= 0)
            {
                AffectSpiritsInRange();
                damageTickTimer = damageTickRate;
            }

            // Spawn fire trails
            trailSpawnTimer -= Time.deltaTime;
            if (trailSpawnTimer <= 0)
            {
                SpawnFireTrail();
                trailSpawnTimer = trailSpawnInterval;
            }
            spawnTimer += 1f * Time.deltaTime;
            if(spawnTimer > spiritSpawnTime && !hasSpawnedSpirit)
            {
                HandleSpiritSpawning();
            }
            // Check for plants to ignite
            IgniteNearbyPlants();
        }
    }
    private void AffectSpiritsInRange()
    {
        Collider[] nearbySpirits = Physics.OverlapSphere(transform.position, interactionRadius);
        foreach (var collider in nearbySpirits)
        {
            SpiritStats spirit = collider.GetComponent<SpiritStats>();
            if (spirit != null && !spirit.spiritData.spiritName.Contains("Fire"))
            {
                spirit.TakeDamage(damageAmount * Time.deltaTime);
            }else if (spirit != null && spirit.spiritData.spiritName.Contains("Fire"))
            {
                FireSpiritBehavior spiritBehavior = spirit.GetComponent<FireSpiritBehavior>();
                StartCoroutine(spiritBehavior.BoostStats(2, 5));
                spawnTimer += 2 * Time.deltaTime;
            }
        }
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
        PlayerManager.Instance.UpdateElementalSpiritCount(spiritObj.GetComponent<SpiritStats>(), 1);
        // Optionally animate the spirit's entrance
        StartCoroutine(AnimateSpiritSpawn(spiritObj));

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
    private void SpawnFireTrail()
    {
        if (fireTrailPrefab == null) return;

        // Spawn in random direction
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        Vector3 spawnPos = transform.position + new Vector3(randomDir.x, 0, randomDir.y) * spreadRadius;

        RaycastHit hit;
        if (Physics.Raycast(spawnPos + Vector3.up * 5f, Vector3.down, out hit))
        {
            Instantiate(fireTrailPrefab, hit.point, Quaternion.identity);
        }
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
    public override bool CanInteract(GameObject spirit)
    {
        SpiritStats stats = spirit.GetComponent<SpiritStats>();
        if (stats == null || beingInteracted) return false;

        string spiritType = stats.spiritData.spiritName;

        switch (currentState)
        {
            case FirePitState.unlit:
                // Fire spirit can relight pit
                return spiritType.Contains("Fire");
            case FirePitState.lit:
                // when low on lifetime, a Fire spirit can restore life time
                if (spiritType.Contains("Fire") && currentLifetime / lifetime < .2) return true;
                // Water spirits puts out pit
                return spiritType.Contains("Water");
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

        // Hid fire viusal
        fireVisual.SetActive(false);
    }
    protected override IEnumerator InteractInternal(GameObject spirit)
    {
        beingInteracted = true;

        SpiritStats stats = spirit.GetComponent<SpiritStats>();
        string spiritType = stats.spiritData.spiritName;

        if (spiritType.Contains("Fire"))
        {
            yield return HandleFireInteraction();
        }
        else if (spiritType.Contains("Water"))
        {
            yield return HandleWaterInteraction();
        }


        yield return null;
        beingInteracted = false;
    }
}
