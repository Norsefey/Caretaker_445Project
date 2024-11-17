using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CleanState : PetState
{
    private float cleaningRate = 4f;
    private float happinessBonus = 0.5f;
    private bool isGrooming = false;
    private bool isAtBath = false;
    private float groomingTimer = 0f;
    private float groomingDuration = 5f;
    private float defaultAgentStoppingDis;


    public CleanState(PetBehavior pet) : base(pet, PetStateType.Clean) { }

    public override void EnterState()
    {
        groomingTimer = 0f;
        pet.GetAgent().isStopped = true;

        if (pet.GetAnimator() != null)
            pet.GetAnimator().SetTrigger("Clean");

        // Try to find nearest Bath
        Transform bathLocation = pet.GetNearestPointOfInterest("Bath")?.location;
        if (bathLocation != null)
        {
            pet.GetAgent().SetDestination(bathLocation.position);
            defaultAgentStoppingDis = pet.GetAgent().stoppingDistance;
            pet.GetAgent().stoppingDistance = 0;
        }
        else
        {
            isGrooming = true;
        }
    }

    public override void UpdateState()
    {
        if (!isAtBath && pet.IsPointOfInterestNearby("Bath"))
        {
            isAtBath = true;
            isGrooming = true;
        }

        if (isGrooming)
        {
            float baseRecovery = isAtBath ? cleaningRate : cleaningRate * 0.5f;

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
