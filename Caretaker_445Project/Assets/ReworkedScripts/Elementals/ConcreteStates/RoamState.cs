using UnityEngine;
using UnityEngine.AI;

public class RoamState : ElementalState
{
    private Vector3 targetPosition;
    public override void Enter(ElementalManager elemental, StateMachine stateMachine)
    {
        base.Enter(elemental, stateMachine);

        targetPosition = GetRandomPointInRadius(elemental.transform.position, elemental.RoamingRange);
        elemental.Agent.isStopped = false;
        elemental.Agent.SetDestination(targetPosition);
        Debug.Log($"{elemental.elementalData.elementalName} is roaming");
    }

    public override void Update()
    {
        if (Random.value < 0.1f)
        {
            CheckForInteractables();
        }
    }
    public override void OnDamageTaken(ElementalManager attacker)
    {
        if (ShouldFight(attacker))
        {
            stateMachine.ChangeState(new PursuitState(attacker.transform));
        }
        else
        {
            stateMachine.ChangeState(new FleeState(attacker.transform));
        }
    }
    public override void CheckTransitions()
    {
        if (CheckForThreats()) return;
        if (elemental.FullCarryCapacity() && elemental.HomeStructure != null)
        {
            stateMachine.ChangeState(new InteractState(elemental.HomeStructure, InteractionType.Contribute));
        }
        // reached destination
        if (Vector3.Distance(elemental.transform.position, targetPosition) <= elemental.Agent.stoppingDistance)
        {
            stateMachine.ChangeState(new IdleState());
        }
    }
    private Vector3 GetRandomPointInRadius(Vector3 center, float radius)
    {
        Vector3 randomPoint = center + Random.insideUnitSphere * radius;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, radius, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return center;
    }
}
