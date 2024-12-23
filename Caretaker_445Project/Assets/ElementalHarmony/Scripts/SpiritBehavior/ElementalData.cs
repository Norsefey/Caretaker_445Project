using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewElementalData", menuName = "Elemental/Elemental Data")]
public class ElementalData : ScriptableObject
{
    public string elementalName;

    // Base stats with variability ranges
    [Header("Stats")]
    public float baseHP;
    public Vector2 hpRange;
    public float hpRecoveryRate;

    public float baseMoveSpeed;
    public Vector2 moveSpeedRange;

    public float baseDamage;
    public Vector2 damageRange; // Range for attack to register

    public float baseStamina;
    public Vector2 staminaRange;
    public float staminaDecayRate;
    public float staminaRecoveryRate;

    [Header("Explore Settings")]
    public float detectionRange = 5f; // Range to detect other spirits or interactables

    [Header("Happiness")]
    public GameObject energyOrb;
    public float happinessIncreaseRate;
    public float happinessDecayRate;

    [Header("Reproduction")]
    public GameObject spawnPrefab;

    [Header("Combat Settings")]
    public float fleeSpeedMultiplier = 1.5f; // Increase speed when fleeing
    public float attackCooldown = 2f; // Time between attacks
}
