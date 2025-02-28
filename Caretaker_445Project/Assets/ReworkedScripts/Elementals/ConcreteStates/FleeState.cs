using UnityEngine;
using UnityEngine.AI;

public class FleeState : ElementalState
{
    private Transform threat;

    public FleeState(Transform threat)
    {
        this.threat = threat;
    }

    public override void Enter(ElementalManager elemental, StateMachine stateMachine)
    {
        base.Enter(elemental, stateMachine);
        elemental.Agent.isStopped = false;
        elemental.Agent.speed = elemental.MoveSpeed * elemental.elementalData.fleeSpeedMultiplier;
        Debug.Log($"{elemental.elementalData.elementalName} is fleeing from {threat.name}");
    }

    public override void Update()
    {
        if (threat == null || !elemental.DecreaseStamina(elemental.elementalData.fleeSpeedMultiplier))
        {
            EndFlee();
            return;
        }

        Vector3 fleeDirection = (elemental.transform.position - threat.position).normalized;
        Vector3 randomOffset = Random.insideUnitSphere * 3f;
        Vector3 fleePosition = elemental.transform.position + (fleeDirection * 10f) + randomOffset;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(fleePosition, out hit, 10f, NavMesh.AllAreas))
        {
            elemental.Agent.SetDestination(hit.position);
        }

    }
    public override void Exit()
    {
        elemental.Agent.speed = elemental.MoveSpeed;
    }
    public override void CheckTransitions()
    {
        if (threat == null)
        {
            EndFlee();
            return;
        }

        float distanceToThreat = Vector3.Distance(elemental.transform.position, threat.position);
        if (distanceToThreat > elemental.DetectionRange * 1.5)
        {
            EndFlee();
        }
    }
    private void EndFlee()
    {
        elemental.Agent.speed = elemental.MoveSpeed;
        //elemental.stats.IncreaseHappiness(5);
        stateMachine.ChangeState(new IdleState());
    }

    public override void OnDamageTaken(ElementalManager attacker)
    {
        // If the new attacker is different than the one we're fleeing from
        if (attacker.transform != threat)
        {
            // Choose the more dangerous threat to flee from
            if (attacker.Damage > threat.GetComponent<ElementalManager>().Damage)
            {
                threat = attacker.transform;
            }
        }
    }
}
