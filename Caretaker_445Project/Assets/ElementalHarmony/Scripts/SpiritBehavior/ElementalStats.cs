using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElementalStats : MonoBehaviour
{
    public float maxHP;
    public float maxStamina;
    [HideInInspector] public float currentHP;
    [HideInInspector] public float currentStamina;
    [HideInInspector] public float moveSpeed;
    [HideInInspector] public float damage;
    public ElementalData elementalData;
    protected ElementalHappiness happiness;
    private ElementalBehavior behavior;
    private void Awake()
    {
        happiness = GetComponent<ElementalHappiness>();
        behavior = GetComponent<ElementalBehavior>();
        InitializeStats();
    }
    private void InitializeStats()
    {
        // Assign random values within the range
        currentHP = Random.Range(elementalData.hpRange.x, elementalData.hpRange.y);
        maxHP = currentHP;
        moveSpeed = Random.Range(elementalData.moveSpeedRange.x, elementalData.moveSpeedRange.y);
        damage = Random.Range(elementalData.damageRange.x, elementalData.damageRange.y);
        currentStamina = Random.Range(elementalData.staminaRange.x, elementalData.staminaRange.y);
        maxStamina = currentStamina;
        // Assign the NavMeshAgent's speed to the elementals's move speed
        GetComponent<UnityEngine.AI.NavMeshAgent>().speed = moveSpeed;
    }
    public void TakeDamage(float amount)
    {
        currentHP -= amount;
        if (currentHP <= 0)
        {
            Die();
        }
        // if sleeping and takes damage check for threats, since by default does not check while sleeping
        if(behavior.currentState == ElementalState.Sleeping)
        {
            behavior.CheckForThreats();
        }
    }
    public float HPPercentage()
    {
        return currentHP / maxHP;
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
            currentHP += elementalData.hpRecoveryRate * multiplier * Time.deltaTime;
        }
    }
    public void IncreaseHappiness(float amount)
    {
        happiness.AdjustHappiness(amount);
    }
    public void DecreaseHappiness(float amount)
    {
        happiness.AdjustHappiness(-amount);
    }
    public bool DecreaseStamina(float multiplier)
    {// deplete stamina over time
        currentStamina -= elementalData.staminaDecayRate * multiplier * Time.deltaTime;

        if(currentStamina <= 0)
            return false;

        return true;
    }
    public void RestoreStamina(float multiplier)
    {// restore Stamina over time
        while(currentStamina < maxStamina)
        {
            currentStamina += elementalData.staminaRecoveryRate * multiplier * Time.deltaTime;
        }
    }
    public void Die()
    {
        Debug.Log($"{elementalData.elementalName} has died!");
        PlayerManager.Instance.UpdateElementalCount(this, -1);
        Destroy(gameObject);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(transform.position, elementalData.detectionRange);
    }
}
