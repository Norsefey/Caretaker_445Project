using System.Collections;
using UnityEngine;
public enum ElementType
{
    Fire,
    Water,
    Plant
}
public enum StructureState
{
    Growing,
    Mature,
    Depleting,
    Depleted
}
[System.Serializable]
public class ResourceAmount
{
    public ElementType resourceType;
    public float currentAmount;

    public ResourceAmount(ElementType type, float amount)
    {
        resourceType = type;
        currentAmount = amount;
    }
}
public class ElementalStructure : Interactable
{
    [Header("Elemental Settings")]
    [SerializeField] private GameObject elementalSpiritPrefab;
    [SerializeField] private StructureState currentState = StructureState.Growing;
    [SerializeField] private float despawnDelay = 30;
    private int structureLevel = 1;

    [Header("Resource Held Settings")]
    [SerializeField] private float baseResourceAmountHeld = 15;
    [SerializeField] private float needScaleMultiplier = 1.3f;// increase amount need by 30% each level up
    [SerializeField] private ResourceAmount resourceHeld;
    [Header("Resource To Upgrade Settings")]
    [SerializeField] private float baseResourceAmountNeeded = 15;
    [SerializeField] private float producedScaleMultiplier = 1.5f;// increase amount produced by 50% each level up
    [SerializeField] private ResourceAmount resourcesNeeded;

    [Header("Owner Settings")]
    private ElementalManager ownerId;
    private bool isDespawning = false;

    [Header("Visual Settings")]
    [SerializeField] private GameObject[] structureVisuals;

    private bool spawnedElementalOne, spawnedElementalTwo, upgradedOwner;


    private void Start()
    {
        UpdateResourceAmounts();
    }

    public void Initialize(ElementalManager owner)
    {
        ownerId = owner;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            UpgradeStructure();
        }
        if (Input.GetKey(KeyCode.D))
        {
            DepleteResources(1);
        }else if (Input.GetKeyUp(KeyCode.D))
        {
            ReturnToDefaultState();
        }
    }

    public void UpgradeStructure()
    {
        if (structureLevel < 3)
        {
            if (isDespawning)
            {
                StopCoroutine("DespawnTimer");
                isDespawning= false;
            }

            structureLevel++;
            UpdateResourceAmounts();
            Debug.Log($"Structure upgraded to level {structureLevel}");

            // Visual or audio feedback for upgrade
            // PlayUpgradeEffect();

            switch (structureLevel)
            {
                case 1:
                    // Spawn Elemental
                    Debug.Log("Spawning Elemental");
                    if (!spawnedElementalOne)
                    {
                        SpawnElemental();
                    }

                    currentState = StructureState.Growing;
                    spawnedElementalOne = true;
                    break;
                case 2:
                    //Upgrade Owner
                    Debug.Log("Upgrading Owner- If alive");
                   
                    if (ownerId != null && !upgradedOwner)
                        ownerId.UpgradeElemental();

                    currentState = StructureState.Growing;
                    upgradedOwner = true;
                    break;
                case 3:
                    // Spawn Elemental
                    Debug.Log("Spawning Elemental");
                    if (!spawnedElementalTwo)
                    {
                        SpawnElemental();
                    }

                    currentState = StructureState.Mature;
                    spawnedElementalTwo = true;
                    // no longer needs resources
                    resourcesNeeded.currentAmount = 0;
                    break;
            }
            ChangeVisual(structureLevel);
            LogResourceChanges();
        }
    }
    private void UpdateResourceAmounts()
    {
        float productionMultiplier = Mathf.Pow(producedScaleMultiplier, structureLevel - 1);
        float consumptionMultiplier = Mathf.Pow(needScaleMultiplier, structureLevel - 1);

        // Update production amounts
        resourceHeld.currentAmount = baseResourceAmountHeld * productionMultiplier;

        // Update consumption amounts
        resourcesNeeded.currentAmount = baseResourceAmountNeeded * consumptionMultiplier;

    }
    public bool TakeInResources(float amount)
    {
        if (currentState == StructureState.Depleting)
            return false;

        resourcesNeeded.currentAmount -= amount;
        bool upgraded = false;

        if (resourcesNeeded.currentAmount <= 0)
        {
            float surplus = -resourcesNeeded.currentAmount;
            resourcesNeeded.currentAmount = 0;

            UpgradeStructure();
            upgraded = true;

            // Handle surplus after upgrade
            if (surplus > 0 && structureLevel < 3)
            {
                TakeInResources(surplus);
            }
        }

        LogResourceChanges();
        return upgraded;
    }
    public float DepleteResources(float amount)
    {
        // no resources to give
        if (currentState == StructureState.Depleted)
            return 0;

        currentState = StructureState.Depleting;

        float availableAmount = Mathf.Min(amount, resourceHeld.currentAmount);
        resourceHeld.currentAmount -= availableAmount;

        if(resourceHeld.currentAmount <= 0)
        {
            resourceHeld.currentAmount = 0;
            UpdateState(StructureState.Depleted);
        }

        return availableAmount;
    }
    private void LogResourceChanges()
    {
        //Debug.Log($"=== Level {structureLevel} Resource Requirements ===");

        //Debug.Log($"Needs {resourcesNeeded.currentAmount} {resourcesNeeded.resourceType} (Base: {resourcesNeeded.baseAmount})");

        //Debug.Log($"Produces {resourcesProduced.currentAmount} {resourcesProduced.resourceType} (Base: {resourcesProduced.baseAmount})");
    }
    public void ReturnToDefaultState()
    {
        switch (structureLevel)
        {
            case 0:
                currentState = StructureState.Depleted; 
                break;
            case 3:
                currentState = StructureState.Mature;
                break;
            default:
                currentState = StructureState.Growing;
                break;
        }
    }
    public void UpdateState(StructureState newState)
    {
        currentState = newState;

        if (currentState == StructureState.Depleted && !isDespawning)
        {
            structureLevel = 0;
            UpdateResourceAmounts();
            ChangeVisual(structureLevel);
            StartCoroutine("DespawnTimer");
        }
    }
    private void ChangeVisual(int index)
    {
       foreach(GameObject visual in structureVisuals)
        {
            visual.SetActive(false);
        }

        structureVisuals[index].SetActive(true);
    }
    private IEnumerator DespawnTimer()
    {
        isDespawning = true;
        yield return new WaitForSeconds(despawnDelay);

        if (currentState == StructureState.Depleted)
        {
            Destroy(gameObject);
        }
        Debug.Log("Despawning: " + isDespawning);
        isDespawning = false;
    }
    private void SpawnElemental()
    {
        Instantiate(elementalSpiritPrefab, transform.position, Quaternion.identity);

        // play VFX
    }
    public override bool CanInteract(ElementalManager elemental, InteractionType interactionType)
    {
        switch (interactionType)
        {
            case InteractionType.Collect:
                // Can collect if not already depleting and has resources
                return currentState != StructureState.Depleted && resourceHeld.currentAmount > 0;

            case InteractionType.Contribute:
                // Can contribute if structure needs resources and not mature yet, and elemental has resources to give
                return structureLevel < 3 && resourcesNeeded.currentAmount > 0 && elemental.ResourceCollected.currentAmount > 0;

            default:
                return false;
        }
    }
    public override bool Interact(ElementalManager elemental, InteractionType interactionType)
    {
        switch (interactionType)
        {
            case InteractionType.Collect:
                return CollectInteraction(elemental);

            case InteractionType.Contribute:
                return ContributeInteraction(elemental);

            default:
                return true; // Unknown interaction type, end interaction
        }
    }
    private bool CollectInteraction(ElementalManager elemental)
    {
        if (currentState == StructureState.Depleted || resourceHeld.currentAmount <= 0)
            return true; // End interaction

        currentState = StructureState.Depleting;
        if(elemental.FullCarryCapacity())
            return true;

        // Fixed depletion amount per interaction
        float depletionAmount = elemental.GatherRate;

        // Deplete resources
        float amountDepleted = DepleteResources(depletionAmount);

        // Add resources to the elemental if we collected any
        if (amountDepleted > 0)
        {
            elemental.AddResource(amountDepleted);
            Debug.Log($"{elemental.elementalData.elementalName} collected {amountDepleted} {resourceHeld.resourceType}");
        }

        // Check if the interaction should complete
        bool interactionComplete = false;

        if (elemental.ResourceCollected.currentAmount >= elemental.MaxCarryAmount ||
            resourceHeld.currentAmount <= 0 ||
            amountDepleted <= 0)
        {
            ReturnToDefaultState();
            interactionComplete = true;
        }

        return interactionComplete;
    }
    private bool ContributeInteraction(ElementalManager elemental)
    {
        // Can't feed if already at max level or doesn't need resources
        if (structureLevel >= 3 || resourcesNeeded.currentAmount <= 0)
            return true; // End interaction

        // Amount elemental will provide this interaction
        float feedAmount = Mathf.Min(elemental.ResourceCollected.currentAmount, elemental.GatherRate);

        if (feedAmount <= 0)
            return true; // Elemental has no resources to give, end interaction

        // Remove resources from elemental
        elemental.RemoveResource(feedAmount);

        // Feed the structure
        bool structureUpgraded = TakeInResources(feedAmount);

        Debug.Log($"{elemental.elementalData.elementalName} fed {feedAmount} to structure");

        // If structure was upgraded or elemental has no more resources, end interaction
        bool interactionComplete = structureUpgraded || elemental.ResourceCollected.currentAmount <= 0;

        return interactionComplete;
    }
}
