using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public abstract class ElementalObject : Interactable
{// Base script for the structures that Elementals will interact with
    // track who is interacting with it, to let them know when this is destroyed
    protected ElementalBehavior interactingElemental;
    protected bool beingInteracted = false;// only allow one interaction at a time

    [Header("Base Settings")]
    public float lifetime = 60f; // If it goes long enough without an interaction it will despawn
    protected float currentLifetime;

    public float interactionRadius = 10f;// range to pull in elementals to interact with it
    protected bool isActive = true;// some objects are able to be disabled through interactions

    [Header("Spirit Spawning")]
    [SerializeField] protected GameObject elementalPrefab;
    [SerializeField] protected ParticleSystem elementalSpawnVFX;// play an effect to let players know it has spawned a elemental
    [SerializeField] protected float elementalSpawnTime = 60f; // how long it takes to try spawning a new elemental
    [SerializeField] protected float elementalSpawnChance = 0.5f; // chance of spawning a elemental , only get one try to prevent overflow
    [SerializeField] protected float elementalSpawnHeight = 1f; // to prevent clipping
    protected float spawnTimer = 0;
    protected bool hasSpawnedElemental = false; // to prevent spawning after trying

    protected virtual void Start()
    {
        currentLifetime = lifetime;
    }

    protected virtual void Update()
    {
        currentLifetime -= Time.deltaTime;
        if (currentLifetime <= 0)
        {
            Despawn();
        }
    }

    protected virtual void Despawn()
    {
        isActive = false;

        // Notify elemental of despawning, to prevent null references on elemental
        if (interactingElemental != null)
        {
            interactingElemental.RemoveInteractable();
        }
        StopAllCoroutines();
        Destroy(gameObject);
    }
    public override IEnumerator Interact(ElementalBehavior elemental)
    {
        if (interactingElemental != elemental)
        {
            interactingElemental = elemental;
        }

        yield return StartCoroutine(InteractInternal(elemental));

        // once interaction is over remove elemental
        interactingElemental = null;
        
    }
    protected abstract IEnumerator InteractInternal(ElementalBehavior elemental);
    protected virtual IEnumerator SpawnElemental()
    {
        yield return null;
    }
}
