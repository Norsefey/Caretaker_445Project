using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class IdleState : PetState
{
    private float idleTimer = 0;
    private float minIdleTime = 1f;
    private float maxIdleTime = 4f;
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
            pet.GetAnimator().SetTrigger("Idle");

    }
    public override void UpdateState()
    {
        idleTimer += Time.deltaTime;
        Debug.Log(idleTimer + " : " + targetIdleTime);
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
