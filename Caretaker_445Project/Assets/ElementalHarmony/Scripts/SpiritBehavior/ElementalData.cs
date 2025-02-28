using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewElementalData", menuName = "Elemental/Elemental Data")]
public class ElementalData : ScriptableObject
{
    public string elementalName;
    // Base stats with variability ranges
    [Header("Health")]
    public Vector2 hpRange;
    public Vector2 hpRecoveryRateRange;
   
    [Header("Movement")]
    public Vector2 moveSpeedRange;
    public float fleeSpeedMultiplier = 1.5f; // Increase speed when fleeing

    [Header("Upgrade Multiplier")]
    public Vector2 upgradeMultiplierRange;
    // i can specify individual stats later

    [Header("Explore Settings")]
    public Vector2 roamingRange;
    public Vector2 detectionRange; // Range to detect other spirits or interactables

   /* [Header("Happiness")]
    public GameObject energyOrb;
    public float happinessIncreaseRate;
    public float happinessDecayRate;*/

    [Header("Resource Management")]
    public Vector2 maxCarryAmountRange;
    public Vector2 gatherRateRange;

    [Header("Combat")]
    public Vector2 damageRange; // Range for attack to register
    public Vector2 attackIntervalRange;
    public Vector2 attackDistanceRange;

    [Header("Stamina")]
    public Vector2 staminaRange;
    public float staminaDecayRate;
    public float staminaRecoveryRate;
}
