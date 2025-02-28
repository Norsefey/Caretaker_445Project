using Mono.Cecil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public class ElementalManager : MonoBehaviour
{
    public ElementType elementType;
    // base stats for all elementals
    public ElementalData elementalData;
    public ElementalUI elementalUI;
    public ElementalCombat elementalCombat;
    [SerializeField] private StateMachine stateMachine;
    protected NavMeshAgent agent;

    //Final Elemental Stats with variation in them set in initialize function
    private float maxHP;
    private float hpRecoveryRate;
    private float currentHP;
    private float maxStamina;
    private float currentStamina;
    private float moveSpeed;
    private float damage;
    private float roamRange;
    private float detectionRange;

    private float maxCarryAmount;
    [SerializeField] private ResourceAmount resourceToGather;
    private float gatheringRate;

    [Header("Structure Management")]
    [SerializeField] private GameObject structurePrefab;
    private float structureDesire = 0f; // Starts at 0, builds up over time
    [SerializeField] private float structureDesireIncreaseRate = 2f;
    [SerializeField] private float structureDesireThreshold = 10f; // When desire exceeds this, try to place
    private ElementalStructure homeStructure;
    public bool hasHomeStructure = false;

    [SerializeField] public LayerMask elementalLayer;
    [SerializeField] public LayerMask interactableLayer;

    public delegate void DamageEvent(ElementalManager attacker);
    public event DamageEvent OnDamageTaken;

    private void Awake()
    {
        elementalUI = GetComponent<ElementalUI>();
        agent = GetComponent<NavMeshAgent>();
        elementalCombat = GetComponent<ElementalCombat>();
        InitializeStats();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            TakeDamage(5);
        }
    }
    private void InitializeStats()
    {
        // Assign random values within the range
        maxHP = Random.Range(elementalData.hpRange.x, elementalData.hpRange.y);
        currentHP = maxHP;

        maxStamina = Random.Range(elementalData.staminaRange.x, elementalData.staminaRange.y);
        currentStamina = maxStamina;

        moveSpeed = Random.Range(elementalData.moveSpeedRange.x, elementalData.moveSpeedRange.y);
        damage = Random.Range(elementalData.damageRange.x, elementalData.damageRange.y);

        maxCarryAmount = Mathf.Round( Random.Range(elementalData.maxCarryAmountRange.x, elementalData.maxCarryAmountRange.y));
        gatheringRate = Random.Range(elementalData.gatherRateRange.x, elementalData.gatherRateRange.y);

        roamRange = Random.Range(elementalData.roamingRange.x, elementalData.roamingRange.y);
        hpRecoveryRate = Random.Range(elementalData.hpRecoveryRateRange.x, elementalData.hpRecoveryRateRange.y);

        elementalCombat.AttackRange = Random.Range(elementalData.attackDistanceRange.x, elementalData.attackDistanceRange.y);
        elementalCombat.AttackCoolDown = Random.Range(elementalData.attackIntervalRange.x, elementalData.attackIntervalRange.y);

        detectionRange = Random.Range(elementalData.detectionRange.x, elementalData.detectionRange.y);
        resourceToGather.currentAmount = 0;
        // Assign the NavMeshAgent's speed to the elementals's move speed
        GetComponent<NavMeshAgent>().speed = moveSpeed;
    }
    public void UpgradeElemental()
    {
        maxHP *= Random.Range(elementalData.upgradeMultiplierRange.x, elementalData.upgradeMultiplierRange.y);
        currentHP = maxHP;
        
        maxStamina *= Random.Range(elementalData.upgradeMultiplierRange.x, elementalData.upgradeMultiplierRange.y);
        currentStamina = maxStamina;

        moveSpeed *= Random.Range(elementalData.upgradeMultiplierRange.x, elementalData.upgradeMultiplierRange.y);
        damage *= Random.Range(elementalData.upgradeMultiplierRange.x, elementalData.upgradeMultiplierRange.y);
        maxCarryAmount *= Random.Range(elementalData.upgradeMultiplierRange.x, elementalData.upgradeMultiplierRange.y);
    }
    // Attacker can be null if a non Elemental wants to do damage
    public void TakeDamage(float amount, ElementalManager attacker = null)
    {
        currentHP -= amount;
        if (currentHP <= 0)
        {
            Die();
        }
        elementalUI.UpdateHPUI(currentHP);
        // Trigger the damage event
        if (attacker != null && OnDamageTaken != null)
        {
            OnDamageTaken(attacker);
        }
    }
    public void Heal(float amount)
    {// Heal a set amount
        currentHP += amount;
        if(currentHP > maxHP)
            currentHP = maxHP;
    }
    public void RestoreHP(float multiplier)
    {// heal over time
        while (currentHP < maxHP)
        {
            currentHP += hpRecoveryRate * multiplier * Time.deltaTime;
        }
    }
    public bool DecreaseStamina(float multiplier)
    {// deplete stamina over time
        currentStamina -= elementalData.staminaDecayRate * multiplier * Time.deltaTime;
        elementalUI.UpdateStaminaUI(currentStamina);

        if (currentStamina <= 0)
            return false;

        return true;
    }
    public void RestoreStamina(float multiplier)
    {// restore Stamina over time
        while(currentStamina < maxStamina)
        {
            currentStamina += elementalData.staminaRecoveryRate * multiplier * Time.deltaTime;
            elementalUI.UpdateStaminaUI(currentStamina);
        }
    }
    public float CalculateSleepChance(float modifier)
    {
        // M = 1 : Linear line
        // M = .5 : Lower Chance
        // M = 2-3 : higher chance

        return 1f - Mathf.Pow(HPPercentage, modifier);
    }
    public void Die()
    {
        Debug.Log($"{elementalData.elementalName} has died!");
        if(PlayerManager.Instance != null)
            PlayerManager.Instance.UpdateElementalCount(this, -1);
        Destroy(gameObject);
    }
    public bool FullCarryCapacity()
    {
        return resourceToGather.currentAmount >= maxCarryAmount;
    }
    public void AddResource(float amount)
    {
        resourceToGather.currentAmount += amount;

        elementalUI.UpdateCarryUI(resourceToGather.currentAmount);

    }
    public void RemoveResource(float amount)
    {
        // do not go below zero
        resourceToGather.currentAmount = Mathf.Max(0, resourceToGather.currentAmount - amount);

        elementalUI.UpdateCarryUI(resourceToGather.currentAmount);

    }
    public void UpdateStructureDesire()
    {
        if (hasHomeStructure) return;

        // Increase desire over time
        structureDesire += structureDesireIncreaseRate;

        // Increase faster when HP is low or when resource is full
        if (HPPercentage < 0.5f)
            structureDesire += 0.1f;

        if (FullCarryCapacity())
            structureDesire += 0.2f;

        // Try to place when desire exceeds threshold
        if (structureDesire > structureDesireThreshold)
        {
            if (Random.value < 0.3f && CanPlaceStructure()) // 30% chance when checking
            {
                if (PlaceStructure())
                {
                    structureDesire = 0f; // Reset desire if successful
                }
                else
                {
                    structureDesire *= 0.8f; // Reduce desire if failed
                }
            }
        }
    }
    public bool CanPlaceStructure()
    {
        // Check for obstacles or other structures in the area
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 5);

        foreach (var collider in hitColliders)
        {
            // Skip the elemental's own collider
            if (collider.gameObject == gameObject) continue;

            // Check if it's another structure
            if (collider.GetComponent<ElementalStructure>() != null)
            {
                return false;
            }

            // Check for terrain obstacles - adjust layers as needed for your project
            if (collider.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
            {
                return false;
            }
        }

        // Check if there's a NavMesh at the placement position
        NavMeshHit hit;
        if (!NavMesh.SamplePosition(transform.position, out hit, 1.0f, NavMesh.AllAreas))
        {
            return false; // No valid NavMesh found at placement position
        }

        return true;
    }
    public bool PlaceStructure()
    {
        // First check if placement is valid
        if (!CanPlaceStructure())
        {
            Debug.Log("Cannot place structure here - obstacles in the way.");
            return false;
        }

        // Check if we already have a home structure
        if (hasHomeStructure)
        {
            Debug.Log("This elemental already has a home structure.");
            return false;
        }

        // Instantiate the structure
        GameObject structure = Instantiate(structurePrefab, transform.position, Quaternion.identity);

        // Get the ElementalStructure component
        homeStructure = structure.GetComponent<ElementalStructure>();

        // Initialize the structure with reference to this elemental
        if (homeStructure != null)
        {
            homeStructure.Initialize(this);
            hasHomeStructure = true;
            Debug.Log($"Structure placed for {elementalData.elementalName}");
            return true;
        }
        else
        {
            Debug.LogError("Failed to get ElementalStructure component from prefab.");
            Destroy(structure);
            return false;
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
    // getters
    public NavMeshAgent Agent { get { return agent; } }
    public float Damage { get { return damage; } }
    public float HPPercentage { get { return currentHP / maxHP; } }
    public float MissingHP { get { return maxHP - currentHP; } }
    public float CurrentHP { get { return currentHP; } }
    public float HPRecoveryRate { get { return hpRecoveryRate; } }
    public float MoveSpeed {  get { return moveSpeed; } }
    public float CurrentStamina { get { return currentStamina; } }
    public float StaminaPercentage { get { return currentStamina / maxHP; } }
    public float RoamingRange { get { return roamRange; } }
    public float DetectionRange { get { return detectionRange; } }
    public float MaxCarryAmount {  get { return maxCarryAmount; } }
    public float GatherRate { get { return gatheringRate; } }
    public ResourceAmount ResourceCollected { get { return resourceToGather; } }
    public ElementalStructure HomeStructure { get { return homeStructure; } }
}
