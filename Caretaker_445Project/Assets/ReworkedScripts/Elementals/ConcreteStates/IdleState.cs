using System.Xml;
using UnityEngine;

public class IdleState : ElementalState
{
    private float idleTimer;
    private float staminaRecoveryRate = .5f;

    public override void Enter(ElementalManager elemental, StateMachine stateMachine)
    {
        base.Enter(elemental, stateMachine);
        
        idleTimer = Random.Range(3, 8);
        elemental.Agent.isStopped = true;
        Debug.Log($"{elemental.elementalData.elementalName} is idle");

        if (!elemental.hasHomeStructure)
        {
            elemental.UpdateStructureDesire();
        }
    }

    public override void Update()
    {
        idleTimer -= Time.deltaTime;
        elemental.RestoreStamina(staminaRecoveryRate);

        CheckForInteractables();
    }

    public override void CheckTransitions()
    {
        if (CheckForThreats()) return;

        if (elemental.FullCarryCapacity() && elemental.HomeStructure != null)
        {
            stateMachine.ChangeState(new InteractState(elemental.HomeStructure, InteractionType.Contribute));
        }

        if (idleTimer <= 0)
        {
            if (Random.value < elemental.CalculateSleepChance(3))
            {
                stateMachine.ChangeState(new SleepState());
            }
            else
            {
                stateMachine.ChangeState(new RoamState());
            }
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
}
