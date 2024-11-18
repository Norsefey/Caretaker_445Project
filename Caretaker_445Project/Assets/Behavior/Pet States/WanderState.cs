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
        {
            pet.GetAnimator().SetTrigger("Walk");
            Debug.Log("Setting Walking Animation");
        }
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

        // exciting to explore
        pet.ModifyNeed(PetNeedType.Happiness, 1f);

        // Slowly decrease energy while walking, hunger rate increases, and get dirty while exploring
        pet.ModifyNeed(PetNeedType.Energy, -2f);
        pet.ModifyNeed(PetNeedType.Hunger, -1);
        pet.ModifyNeed(PetNeedType.Cleanliness, -0.5f);

    }
    public override void ExitState()
    {
        pet.GetAnimator().ResetTrigger("Walk");
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
