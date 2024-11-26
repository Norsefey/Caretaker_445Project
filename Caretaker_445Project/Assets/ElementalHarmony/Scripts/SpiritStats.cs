using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiritStats : MonoBehaviour
{
    private float maxHP;
    private float maxStamina;
    [HideInInspector] public float currentHP;
    [HideInInspector] public float currentStamina;
    [HideInInspector] public float moveSpeed;
    [HideInInspector] public float damage;
    public SpiritData spiritData;
    protected SpiritHappiness happiness;
    private void Awake()
    {
        happiness = GetComponent<SpiritHappiness>();
        InitializeStats();
    }
    private void InitializeStats()
    {
        // Assign random values within the range
        currentHP = Random.Range(spiritData.hpRange.x, spiritData.hpRange.y);
        maxHP = currentHP;
        moveSpeed = Random.Range(spiritData.moveSpeedRange.x, spiritData.moveSpeedRange.y);
        damage = Random.Range(spiritData.damageRange.x, spiritData.damageRange.y);
        currentStamina = Random.Range(spiritData.staminaRange.x, spiritData.staminaRange.y);
        maxStamina = currentStamina;
        // Assign the NavMeshAgent's speed to the spirit's move speed
        GetComponent<UnityEngine.AI.NavMeshAgent>().speed = moveSpeed;
    }
    public void TakeDamage(float amount)
    {
        currentHP -= amount;
        if (currentHP <= 0)
        {
            Die();
        }
    }
    public float HPPercentage()
    {
        return currentHP / maxHP;
    }
    public void Heal(float amount)
    {
        currentHP += amount;
        if(currentHP > maxHP)
            currentHP = maxHP;
    }
    public IEnumerator RestoreHP()
    {
        while (currentHP < maxHP)
        {
            currentHP += spiritData.hpRecoveryRate * Time.deltaTime;
            yield return null;
        }

        yield return null;
    }
    public void IncreaseHappiness(float amount)
    {
        happiness.AdjustHappiness(amount);
    }
    public void DecreaseHappiness(float amount)
    {
        happiness.AdjustHappiness(-amount);
    }
    public bool DecreaseStamina()
    {
        currentStamina -= spiritData.staminaDecayRate * Time.deltaTime;

        if(currentStamina <= 0)
            return false;

        return true;
    }
    public IEnumerator RestoreStamina()
    {
        while(currentStamina < maxStamina)
        {
            currentStamina += spiritData.staminaRecoveryRate * Time.deltaTime;
            yield return null;
        }

        yield return null;
    }
    private void Die()
    {
        Debug.Log($"{spiritData.spiritName} has died!");
        Destroy(gameObject);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(transform.position, spiritData.detectionRange);
    }
}
