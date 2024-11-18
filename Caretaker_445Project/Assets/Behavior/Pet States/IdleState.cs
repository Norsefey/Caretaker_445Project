using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class IdleState : PetState
{
    private float idleTimer = 0;
    private float minIdleTime = 2f;
    private float maxIdleTime = 5f;
    private float targetIdleTime;

    public IdleState(PetBehavior pet) : base(pet, PetStateType.Idle) 
    {
    }
    public override void EnterState()
    {
        pet.GetAgent().isStopped = true;
        targetIdleTime = Random.Range(minIdleTime, maxIdleTime);
        idleTimer = 0f;

        if(pet.GetAnimator() != null)
        {
            pet.GetAnimator().SetTrigger("Idle");
            Debug.Log("Setting Idle Animation");

        }
    }
    public override void UpdateState()
    {
        idleTimer += Time.deltaTime;
        //Debug.Log(idleTimer + " : " + targetIdleTime);
        if(idleTimer > targetIdleTime)
        {
            CheckRandomRules();
        }

        // Reduce decay rate while in idle mode, but not as much as rest state
        pet.ModifyNeed(PetNeedType.Energy, 0.5f);
        // Boring to be in Idle
        pet.ModifyNeed(PetNeedType.Happiness, -2f);
    }
    public override void ExitState()
    {
        pet.GetAnimator().ResetTrigger("Idle");

        idleTimer = 0;
        pet.GetAgent().isStopped = false;
    }

    public override void CheckRandomRules()
    {
        if (!pet.CheckRandomStateTransitions())
        {
            EnterState();
        }
    }
}
