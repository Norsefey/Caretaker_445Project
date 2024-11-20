using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FeedingState : PetState
{
    private float feedingRate = 5f;
    private float energyRate = 1f;
    private float happinessBonus = 1f;
    private float cleanlinessReduction = -1f;
    private bool isAtFood = false;

    public FeedingState(PetBehavior pet) : base(pet, PetStateType.SeekFood) { }

    public override void EnterState()
    {
        pet.GetAgent().isStopped = false;
        if (pet.GetAnimator() != null)
            pet.GetAnimator().SetTrigger("Eat");

        Transform foodLocation = pet.GetNearestPointOfInterest("Food")?.location;
        if (foodLocation != null)
            pet.GetAgent().SetDestination(foodLocation.position);
    }

    public override void UpdateState()
    {
        if (!isAtFood && pet.IsPointOfInterestNearby("Food"))
        {
            isAtFood = true;
            pet.GetAgent().isStopped = true;
        }

        if (isAtFood)
        {
            pet.ModifyNeed(PetNeedType.Hunger, feedingRate);
            pet.ModifyNeed(PetNeedType.Energy, energyRate);
            pet.ModifyNeed(PetNeedType.Happiness, happinessBonus);
            pet.ModifyNeed(PetNeedType.Cleanliness, cleanlinessReduction);
        }

        CheckRandomRules();
    }

    public override void ExitState()
    {
        isAtFood = false;
        pet.GetAgent().isStopped = false;
    }

    public override void CheckRandomRules()
    {
        pet.CheckRandomStateTransitions();
    }
}
