using System.Collections;
using System.Collections.Generic;
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
    public string resourceType;
    public float baseAmount;
    public float currentAmount;

    public ResourceAmount(string type, float amount)
    {
        resourceType = type;
        baseAmount = amount;
        currentAmount = amount;
    }
}
public class ElementalStructure : MonoBehaviour
{
    [Header("Elemental Settings")]
    [SerializeField] private ElementType elementType;
    [SerializeField] private StructureState currentState = StructureState.Growing;
    [SerializeField] private float despawnDelay = 30;
    private int structureLevel = 1;

    [Header("Resource Settings")]
    [SerializeField] private ResourceAmount resourceHeld;
    [SerializeField] private ResourceAmount resourcesNeeded;
    [SerializeField] private float producedScaleMultiplier = 1.5f;// increase amount produced by 50% each level up
    [SerializeField] private float needScaleMultiplier = 1.3f;// increase amount need by 30% each level up

    [Header("Owner Settings")]
    [SerializeField] private ElementalBehavior ownerId;
    private bool isDespawning = false;

    [Header("Visual Settings")]
    [SerializeField] private GameObject[] structureVisuals;

    private bool spawnedElementalOne, spawnedElementalTwo, upgradedOwner;


    private void Start()
    {
        UpdateResourceAmounts();
    }

    public void Initialize(ElementType element, ElementalBehavior owner)
    {
        elementType = element;
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
                    currentState = StructureState.Growing;
                    spawnedElementalOne = true;
                    break;
                case 2:
                    //Upgrade Owner
                    Debug.Log("Upgrading Owner- If alive");
                    currentState = StructureState.Growing;
                    upgradedOwner = true;
                    break;
                case 3:
                    // Spawn Elemental
                    Debug.Log("Spawning Elemental");
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
        resourceHeld.currentAmount = resourceHeld.baseAmount * productionMultiplier;

        // Update consumption amounts
        resourcesNeeded.currentAmount = resourcesNeeded.baseAmount * consumptionMultiplier;

    }
    public bool TakeInResources(float amount)
    {
        if(currentState == StructureState.Depleting)
            return false;

        resourcesNeeded.currentAmount -= amount;
        
        if (resourcesNeeded.currentAmount <= 0)
        {
            float temp = resourcesNeeded.currentAmount -= amount;

            UpgradeStructure();

            // surplus is taken in after upgrade
            if(temp < 0)
            {
                TakeInResources(temp);
            }
        }

        LogResourceChanges();
        return true;
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
    
    // Getters
    public ElementType GetElementType() => elementType;
    public StructureState GetState() => currentState;
    public int GetLevel() => structureLevel;
    public ElementalBehavior GetOwnerId() => ownerId;
    public ResourceAmount GetResourcesProduced() => resourceHeld;
}
