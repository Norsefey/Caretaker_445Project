using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public class WanderState : PetState
{
    private float wanderRadius = 20f;
    private float minWanderTime = 5f;
    private float maxWanderTime = 10f;
    private float wanderTimer;
    private float targetWanderTime;
    public WanderState(PetBehavior pet) : base(pet, PetStateType.Wander) { }
    public override void EnterState()
    {
        wanderTimer = 0f;
        targetWanderTime = Random.Range(minWanderTime, maxWanderTime);
        SetNewWanderDestination();
        if(pet.GetAnimator() != null)
            pet.GetAnimator().SetTrigger("Walk");
    }
    public override void UpdateState()
    {
        wanderTimer += Time.deltaTime;

        // If we've reached destination or haven't moved in a while, get new destination
        // Give pet time to start moving before checking if it is stuck
        if (pet.GetAgent().remainingDistance < 0.1f || (pet.GetAgent().velocity.magnitude < 0.1f && wanderTimer > targetWanderTime / 2))
        {
            CheckRandomRules();
        }

        // Slowly decrease energy while walking
        pet.ModifyNeed(PetNeedType.Energy, -2f);
        // exciting to explore
        pet.ModifyNeed(PetNeedType.Happiness, 1f);

    }
    public override void ExitState()
    {
        pet.GetAgent().ResetPath();
    }

    private void SetNewWanderDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += pet.transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, 1))
        {
            pet.GetAgent().SetDestination(hit.position);
        }
    }
    public override void CheckRandomRules()
    {
        if (!pet.CheckRandomStateTransitions())
        {
            EnterState();
        }
    }
}
