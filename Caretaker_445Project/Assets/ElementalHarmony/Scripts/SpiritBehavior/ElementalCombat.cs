using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElementalCombat : MonoBehaviour
{
    [Header("Combat Settings")]
    public float attackRange = 5f;
    public float attackCooldown = 2f;
    public float attackAngle = 45f; // Angle in which Elemental can deal damage

    private float nextAttackTime = 0;
    private ElementalStats stats;

    private void Start()
    {
        stats = GetComponent<ElementalStats>();
    }

    public bool AttackIntervalCheck()
    {
        return nextAttackTime <= Time.time;
    }

    public void Attack(ElementalStats target)
    {
        if (!AttackIntervalCheck()) return;

        // Check if target is within attack range and angle
        Vector3 directionToTarget = (target.transform.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToTarget);

        if (Vector3.Distance(transform.position, target.transform.position) <= attackRange &&
            angle <= attackAngle * 0.5f)
        {
            Debug.Log($"{stats.elementalData.elementalName} attacks {target.elementalData.elementalName} for {stats.damage} damage!");
            target.TakeDamage(stats.damage);
            nextAttackTime = Time.time + attackCooldown;

            if (target.currentHP <= 0)
            {
                HandleTargetDefeated(target);
            }
        }
    }

    private void HandleTargetDefeated(ElementalStats target)
    {
        Debug.Log($"{target.elementalData.elementalName} has been defeated!");

        // give some happiness when combat is successful
        stats.IncreaseHappiness(15);
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
