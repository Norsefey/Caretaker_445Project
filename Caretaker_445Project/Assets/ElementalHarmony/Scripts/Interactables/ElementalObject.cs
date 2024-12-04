using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public abstract class ElementalObject : MonoBehaviour, IInteractable
{
    protected List<GameObject> interactingSpirits = new List<GameObject>();

    [Header("Base Settings")]
    public float lifetime = 30f;
    public float interactionRadius = 2f;
    public ParticleSystem activeVFX;
    public float despawnDuration = 2f;

    protected float currentLifetime;
    protected bool isActive = true;
    protected bool beingInteracted = false;

    [Header("Spirit Spawning")]
    [SerializeField] protected GameObject spiritPrefab;
    [SerializeField] protected ParticleSystem spiritSpawnVFX;
    [SerializeField] protected float spiritSpawnTime = 60f; // how long it takes to try spawning a new elemental
    [SerializeField] protected float spiritSpawnChance = 0.5f; // chance of spawning a elemental , only get one try
    [SerializeField] protected float spiritSpawnHeight = 1f; // to prevent clipping
    protected float spawnTimer = 0;
    protected bool hasSpawnedSpirit = false; // to prevent spawning after trying

    protected virtual void Start()
    {
        currentLifetime = lifetime;
        if (activeVFX != null) activeVFX.Play();
    }

    protected virtual void Update()
    {
        if (!isActive) return;

        currentLifetime -= Time.deltaTime;
        if (currentLifetime <= 0)
        {
            StartCoroutine(DespawnRoutine());
        }
    }

    protected virtual IEnumerator DespawnRoutine()
    {
        isActive = false;

        // Notify spirits before starting despawn effects
        foreach (var spirit in interactingSpirits)
        {
            if (spirit != null)
            {
                var spiritBehavior = spirit.GetComponent<SpiritBehavior>();
                if (spiritBehavior != null)
                {
                    spiritBehavior.RemoveInteractable();
                }
            }
        }

        if (activeVFX != null)
        {
            var emission = activeVFX.emission;
            float startRate = emission.rateOverTime.constant;

            float elapsed = 0f;
            while (elapsed < despawnDuration)
            {
                elapsed += Time.deltaTime;
                emission.rateOverTime = startRate * (1 - elapsed / despawnDuration);
                yield return null;
            }

            activeVFX.Stop();
        }
        StopAllCoroutines();
        Destroy(gameObject);
    }
    public abstract bool CanInteract(GameObject spirit);
    public Vector3 GetInteractionPoint() 
    {
        return transform.position;
    }
    public virtual IEnumerator Interact(GameObject spirit)
    {
        if (!interactingSpirits.Contains(spirit))
        {
            interactingSpirits.Add(spirit);
        }

        if(this == null)
            yield return null;
        else
            yield return StartCoroutine(InteractInternal(spirit));

        if (this != null && interactingSpirits.Contains(spirit))
        {
            interactingSpirits.Remove(spirit);
        }
    }
    protected abstract IEnumerator InteractInternal(GameObject spirit);
    protected virtual IEnumerator SpawnElemental()
    {
        yield return null;
    }
}
