using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class AttackState : ElementalState
{
    private Transform target;

    public AttackState(Transform target)
    {
        this.target = target;
    }

    public override void Enter(ElementalManager elemental, StateMachine stateMachine)
    {
        base.Enter(elemental, stateMachine);
        elemental.Agent.isStopped = true;
    }
    public override void Update()
    {
        if (target == null)
        {
            stateMachine.ChangeState(new IdleState());
            return;
        }

        // Face target
        Vector3 directionToTarget = (target.position - elemental.transform.position).normalized;
        elemental.transform.forward = directionToTarget;

        if (elemental.elementalCombat.AttackIntervalCheck())
        {
            ElementalManager targetElemental = target.GetComponent<ElementalManager>();
            if (targetElemental != null)
            {
                elemental.elementalCombat.Attack(targetElemental);
            }
        }
    }
    public override void CheckTransitions()
    {
        if (target == null) return;

        float distanceToTarget = Vector3.Distance(elemental.transform.position, target.position);
        if (distanceToTarget > elemental.elementalCombat.AttackRange)
        {
            stateMachine.ChangeState(new PursuitState(target));
        }
    }
    public override void OnDamageTaken(ElementalManager attacker)
    {
        // If health gets very low during combat or a stronger attacker appears
        if (elemental.HPPercentage <=  0.45f ||
            (attacker.transform != target && attacker.Damage > elemental.Damage * 1.5f))
        {
            stateMachine.ChangeState(new FleeState(attacker.transform));
        }
    }
}
