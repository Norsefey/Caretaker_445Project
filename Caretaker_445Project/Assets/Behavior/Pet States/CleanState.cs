using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CleanState : PetState
{
    private float cleaningRate = 3f;
    private float happinessBonus = 0.5f;
    private bool isGrooming = false;
    private float groomingTimer = 0f;
    private float groomingDuration = 5f;

    public CleanState(PetBehavior pet) : base(pet, PetStateType.Clean) { }

    public override void EnterState()
    {
        isGrooming = true;
        groomingTimer = 0f;
        pet.GetAgent().isStopped = true;

        if (pet.GetAnimator() != null)
            pet.GetAnimator().SetTrigger("Clean");
    }

    public override void UpdateState()
    {
        if (isGrooming)
        {
            groomingTimer += Time.deltaTime;
            pet.ModifyNeed(PetNeedType.Cleanliness, cleaningRate);
            pet.ModifyNeed(PetNeedType.Happiness, happinessBonus);

            if (groomingTimer >= groomingDuration)
                CheckRandomRules();
        }
    }

    public override void ExitState()
    {
        isGrooming = false;
        groomingTimer = 0f;
        pet.GetAgent().isStopped = false;
    }

    public override void CheckRandomRules()
    {
        pet.CheckRandomStateTransitions();
    }
}
