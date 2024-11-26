using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayState : PetState
{
    private float happinessRate = 4f;
    private float cleanlinessReduction = -1.5f;
    private float energyDrain = -1f;
    private bool isAtToy = false;

    public PlayState(PetBehavior pet) : base(pet, PetStateType.Play) { }

    public override void EnterState()
    {
        pet.GetAgent().isStopped = false;
        if (pet.GetAnimator() != null)
            pet.GetAnimator().SetTrigger("Walk");

        Transform toyLocation = pet.GetNearestPointOfInterest("Toy")?.location;
        if (toyLocation != null)
            pet.GetAgent().SetDestination(toyLocation.position);
    }

    public override void UpdateState()
    {
        if (!isAtToy && pet.IsPointOfInterestNearby("Toy"))
        {
            if (pet.GetAnimator() != null)
            {
                pet.GetAnimator().ResetTrigger("Walk");
                pet.GetAnimator().SetTrigger("Play");
            }
            isAtToy = true;
            pet.GetAgent().isStopped = true;
        }

        if (isAtToy)
        {
            pet.ModifyNeed(PetNeedType.Happiness, happinessRate);
            pet.ModifyNeed(PetNeedType.Cleanliness, cleanlinessReduction);
            pet.ModifyNeed(PetNeedType.Energy, energyDrain);
        }

        CheckRandomRules();
    }

    public override void ExitState()
    {
        isAtToy = false;
        pet.GetAgent().isStopped = false;
        pet.GetAnimator().ResetTrigger("Play");
    }

    public override void CheckRandomRules()
    {
        pet.CheckRandomStateTransitions();
    }
}
