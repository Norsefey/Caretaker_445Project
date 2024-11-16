using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SleepState : PetState
{
    private float recoveryRate = 4f;
    private float happinessBonus = 1f;
    private bool isAtBed = false;

    public SleepState(PetBehavior pet) : base(pet, PetStateType.Sleep) { }

    public override void EnterState()
    {
        pet.GetAgent().isStopped = false;
        if (pet.GetAnimator() != null)
            pet.GetAnimator().SetTrigger("Sleep");

        // Try to find nearest bed
        Transform bedLocation = pet.GetNearestPointOfInterest("Bed")?.location;
        if (bedLocation != null)
            pet.GetAgent().SetDestination(bedLocation.position);
    }

    public override void UpdateState()
    {
        if (!isAtBed && pet.IsPointOfInterestNearby("Bed"))
        {
            isAtBed = true;
            pet.GetAgent().isStopped = true;
        }

        // Apply recovery rates
        float baseRecovery = isAtBed ? recoveryRate : recoveryRate * 0.5f;
        pet.ModifyNeed(PetNeedType.Energy, baseRecovery);
        pet.ModifyNeed(PetNeedType.Happiness, happinessBonus);

        CheckRandomRules();
    }

    public override void ExitState()
    {
        isAtBed = false;
        pet.GetAgent().isStopped = false;
        if (pet.GetAnimator() != null)
            pet.GetAnimator().SetTrigger("Wake");
    }

    public override void CheckRandomRules()
    {
        pet.CheckRandomStateTransitions();
    }
}
