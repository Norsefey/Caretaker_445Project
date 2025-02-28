using UnityEngine;

public class PursuitState : ElementalState
{
    private Transform target;
    
    public PursuitState(Transform target)
    {
        this.target = target;
    }

    public override void Enter(ElementalManager elemental, StateMachine stateMachine)
    {
        base.Enter(elemental, stateMachine);
        elemental.Agent.isStopped = false;
        Debug.Log($"{elemental.elementalData.elementalName} is pursuing {target.name}");
    }
    public override void Update()
    {
        if (target == null || !elemental.DecreaseStamina(1))
        {
            EndPursuit();
        }

        elemental.Agent.SetDestination(target.position);
    }

    public override void CheckTransitions()
    {
        if(target == null) return;

        float distanceToTarget = Vector3.Distance(elemental.transform.position, target.position);

        if(distanceToTarget > elemental.DetectionRange)
        {
            EndPursuit();
        }else if(distanceToTarget <= elemental.elementalCombat.AttackRange)
        {
            // enter Attack State
            stateMachine.ChangeState(new AttackState(target));
        }
    }
    public override void OnDamageTaken(ElementalManager attacker)
    {
        // If the damage is from someone other than the current target, and it's significant
        if (attacker.transform != target && attacker.Damage > elemental.Damage * 1.5f)
        {
            stateMachine.ChangeState(new FleeState(attacker.transform));
        }
    }
    private void EndPursuit()
    {
        stateMachine.ChangeState(new IdleState());

    }
}
