using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ObeyingState : PetState
{
    private Vector3 targetPosition;
    private GameObject targetObject;
    private bool isMovingToTarget = false;
    private float interactionRadius = 1.5f;

    public ObeyingState(PetBehavior pet) : base(pet, PetStateType.Interact) { }

    public override void EnterState()
    {
        pet.GetAgent().isStopped = false;
        if (pet.GetAnimator() != null)
            pet.GetAnimator().SetTrigger("Walk");

        isMovingToTarget = false;
        Debug.Log("entering State: Moving to Target?" + isMovingToTarget);
    }

    public override void UpdateState()
    {
        if (!isMovingToTarget) return;
        NavMeshAgent agent = pet.GetAgent();

        float distanceToTarget = Vector3.Distance(pet.transform.position, targetPosition);
        if (distanceToTarget <= interactionRadius ||
            (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance))
        {

            HandleTargetReached();
        }

        // Still moving - give some happiness for being obedient
        pet.ModifyNeed(PetNeedType.Happiness, 0.5f);
    }

    private void HandleTargetReached()
    {
        isMovingToTarget = false;
        if (targetObject != null)
        {
            // Handle different types of interactions based on the object's tag
            switch (targetObject.tag)
            {
                case PlayerCommand.bedTag:
                    pet.ChangeState(PetStateType.Sleep);
                    break;
                case PlayerCommand.foodTag:
                    pet.ChangeState(PetStateType.SeekFood);
                    break;
                case PlayerCommand.toyTag:
                    pet.ChangeState(PetStateType.Play);
                    break;
                case PlayerCommand.bathTag:
                    pet.ChangeState(PetStateType.Clean);
                    break;
                default:
                    pet.ChangeState(PetStateType.Idle);
                    break;
            }
        }
        else
        {
            // If there was no specific object (just a position), go to idle
            pet.ChangeState(PetStateType.Idle);
        }
    }

    public override void ExitState()
    {
        pet.GetAgent().isStopped = false;
        isMovingToTarget = false;
        targetObject = null;
    }
    public override void CheckRandomRules()
    {
        /*if (!pet.CheckRandomStateTransitions())
        {
            EnterState();
        }*/
    }
    public void SetDestination(Vector3 position, GameObject targetObj = null)
    {
        targetPosition = position;
        targetObject = targetObj;

        NavMeshAgent agent = pet.GetAgent();

        // If we have a target object, adjust the target position to account for the interaction radius
        if (targetObj != null)
        {
            // Calculate the direction from the target to where we want the pet to stop
            Vector3 directionToTarget = (agent.transform.position - position).normalized;
            // Adjust the target position to be slightly away from the object
            Vector3 adjustedPosition = position + (directionToTarget * (interactionRadius * 0.8f));

            // Sample the nearest valid position on the NavMesh
            if (NavMesh.SamplePosition(adjustedPosition, out NavMeshHit hit, interactionRadius, NavMesh.AllAreas))
            {
                position = hit.position;
            }
        }

        agent.stoppingDistance = targetObj != null ? interactionRadius * 0.8f : 0.1f;
        agent.SetDestination(position);
        isMovingToTarget = true;
        Debug.Log("Set Destination Moving to Target?" + isMovingToTarget);
    }
}
