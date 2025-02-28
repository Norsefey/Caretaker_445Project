using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElementalCombat : MonoBehaviour
{
    private float attackRange = 5f;
    private float attackCooldown = 2f;
    private float attackAngle = 45f; // Angle in which Elemental can deal damage

    private float nextAttackTime = 0;
    private ElementalManager stats;

    private void Start()
    {
        stats = GetComponent<ElementalManager>();
    }

    public bool AttackIntervalCheck()
    {
        return nextAttackTime <= Time.time;
    }

    public void Attack(ElementalManager target)
    {
        if (!AttackIntervalCheck()) return;

        // Check if target is within attack range and angle
        Vector3 directionToTarget = (target.transform.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToTarget);

        if (Vector3.Distance(transform.position, target.transform.position) <= attackRange &&
            angle <= attackAngle * 0.5f)
        {
            Debug.Log($"{stats.elementalData.elementalName} attacks {target.elementalData.elementalName} for {stats.Damage} damage!");
            target.TakeDamage(stats.Damage);
            nextAttackTime = Time.time + attackCooldown;

            if (target.CurrentHP <= 0)
            {
                HandleTargetDefeated(target);
            }
        }
    }
    private void HandleTargetDefeated(ElementalManager target)
    {
        Debug.Log($"{target.elementalData.elementalName} has been defeated!");
    }

    public float AttackRange { get {  return attackRange; } set { attackRange = value; } }
    public float AttackCoolDown { set { attackCooldown = value; } }
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
