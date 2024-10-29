using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class WanderState : PetState
{
    private float wanderRadius = 10f;
    private float minWanderTime = 5f;
    private float maxWanderTime = 15f;
    private float wanderTimer;
    private float targetWanderTime;
    public WanderState(PetBehavior pet) : base(pet, PetStateType.Wander) { }
    public override void EnterState()
    {
        wanderTimer = 0f;
        targetWanderTime = Random.Range(minWanderTime, maxWanderTime);
        SetNewWanderDestination();
        if(pet.anime != null)
            pet.anime.SetTrigger("Walk");
    }
    public override void UpdateState()
    {
        wanderTimer += Time.deltaTime;

        // If we've reached destination or haven't moved in a while, get new destination
        if (agent.remainingDistance < 0.1f || agent.velocity.magnitude < 0.1f)
        {
            SetNewWanderDestination();
        }

        // Slowly decrease energy while walking
        pet.needs.ModifyNeed(PetNeedType.Energy, -1f * Time.deltaTime);
    }
    public override void ExitState()
    {
        agent.ResetPath();
    }

    private void SetNewWanderDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += pet.transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, 1))
        {
            agent.SetDestination(hit.position);
        }
    }

}
