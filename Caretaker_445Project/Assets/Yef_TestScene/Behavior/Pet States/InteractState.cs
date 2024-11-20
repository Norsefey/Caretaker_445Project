using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractState : PetState
{
    private float happinessBoost = 3f;
    private float interactionTimer = 0f;
    private float interactionDuration = 3f;
    private bool isInteracting = false;

    public InteractState(PetBehavior pet) : base(pet, PetStateType.Interact) { }

    public override void EnterState()
    {
        pet.GetAgent().isStopped = true;
        isInteracting = true;
        interactionTimer = 0f;

        if (pet.GetAnimator() != null)
            pet.GetAnimator().SetTrigger("Interact");
    }

    public override void UpdateState()
    {
        if (isInteracting)
        {
            interactionTimer += Time.deltaTime;
            pet.ModifyNeed(PetNeedType.Happiness, happinessBoost);

            if (interactionTimer >= interactionDuration)
                CheckRandomRules();
        }
    }

    public override void ExitState()
    {
        isInteracting = false;
        interactionTimer = 0f;
        pet.GetAgent().isStopped = false;
    }

    public override void CheckRandomRules()
    {
        pet.CheckRandomStateTransitions();
    }
}
