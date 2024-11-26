using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiritCombat : MonoBehaviour
{
    [Header("Combat Settings")]
    public float attackRange = 5f;
    public float attackCooldown = 2f;
    public float attackAngle = 45f; // Angle in which spirit can attack

    private float nextAttackTime = 0;
    private SpiritStats stats;

    private void Start()
    {
        stats = GetComponent<SpiritStats>();
    }

    public bool CanAttack()
    {
        return nextAttackTime <= Time.time;
    }

    public void Attack(SpiritStats target)
    {
        if (!CanAttack()) return;

        // Check if target is within attack range and angle
        Vector3 directionToTarget = (target.transform.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToTarget);

        if (Vector3.Distance(transform.position, target.transform.position) <= attackRange &&
            angle <= attackAngle * 0.5f)
        {
            Debug.Log($"{stats.spiritData.spiritName} attacks {target.spiritData.spiritName} for {stats.damage} damage!");
            target.TakeDamage(stats.damage);
            nextAttackTime = Time.time + attackCooldown;

            if (target.currentHP <= 0)
            {
                HandleTargetDefeated(target);
            }
        }
    }

    private void HandleTargetDefeated(SpiritStats target)
    {
        Debug.Log($"{target.spiritData.spiritName} has been defeated!");

        // Drop energy orbs
        //int orbCount = Random.Range(1, 3);
        //EnergyOrbManager.Instance.SpawnOrb(target.transform.position, orbCount);
        // give some happiness when combat is successful
        stats.IncreaseHappiness(15);
        Destroy(target.gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw attack angle
        Vector3 rightDir = Quaternion.Euler(0, attackAngle * 0.5f, 0) * transform.forward;
        Vector3 leftDir = Quaternion.Euler(0, -attackAngle * 0.5f, 0) * transform.forward;
        Debug.DrawRay(transform.position, rightDir * attackRange, Color.yellow);
        Debug.DrawRay(transform.position, leftDir * attackRange, Color.yellow);
    }
}
