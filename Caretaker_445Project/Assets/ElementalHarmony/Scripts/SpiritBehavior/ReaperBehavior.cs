using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReaperBehavior : ElementalBehavior
{
    public float lifeTime = 15;
    protected float spawnTime;
    protected override void Start()
    {
        spawnTime = Time.time;
        agent.speed = stats.moveSpeed;
        StartCoroutine(StateHandler());
    }

    protected override IEnumerator StateHandler()
    {
        while (true)
        {
            if (lifeTime <= Time.time - spawnTime)
            {
                Destroy(gameObject);
            }
            switch (currentState)
            {
                case ElementalState.Idle:
                    yield return HandleIdleState();
                    break;

                case ElementalState.Roaming:
                    yield return HandleRoamState();
                    break;

                case ElementalState.Sleeping:
                    yield return HandleSleepState();
                    break;

                case ElementalState.Interacting:
                    yield return HandleInteractState();
                    break;

                case ElementalState.Pursuing:
                    yield return HandlePursuitState();
                    break;

                case ElementalState.Attacking:
                    yield return HandleAttackState();
                    break;

                case ElementalState.Fleeing:
                    yield return HandleFleeState();
                    break;
            }
            yield return null;
        }
    }

    protected override IEnumerator HandleIdleState()
    {

        Debug.Log($"{elementalData.elementalName} is idle");
        agent.isStopped = true;
        stateTimer = idleTime;

        while (stateTimer > 0)
        {
            // Check for threats
            if (CheckForThreats()) yield break;

            // Check for interactables
            if (Random.value < 0.1f && CheckForInteractables()) yield break;

            stateTimer -= Time.deltaTime;
            stats.RestoreStamina(.5f);

            yield return null;
        }

        // If low on stamina or HP, go to sleep to recover, with some random chance if not low on anything
        if (stats.HPPercentage() < .25f || stats.currentStamina < stats.maxStamina / 2 || Random.value < sleepChance)
            TransitionToState(ElementalState.Sleeping);
        else// from Idle go explore
            TransitionToState(ElementalState.Roaming);
    }
    protected override bool CheckForInteractables()
    {
        // Prioritize burning plants
        Collider[] nearbyInteractables = Physics.OverlapSphere(transform.position, stats.elementalData.detectionRange,
            interactableLayer);

        foreach (var collider in nearbyInteractables)
        {
            Interactable interactable = collider.GetComponent<Interactable>();
            if (interactable != null)
            {
                currentInteractable = interactable;
                if(currentState != ElementalState.Pursuing || currentState != ElementalState.Fleeing)
                    TransitionToState(ElementalState.Interacting);
                return true;
            }
        }

        return base.CheckForInteractables();
    }
    protected override IEnumerator HandleInteractState()
    {
        if (currentInteractable == null)
        {
            Debug.LogWarning($"{elementalData.elementalName}: Interactable became null before getting interaction point");

            TransitionToState(ElementalState.Idle);
            yield break;
        }

        Vector3 targetPos = currentInteractable.transform.position;


        Debug.Log($"{elementalData.elementalName} is interacting with {currentInteractable}");
        stateTimer = 10;
        agent.isStopped = false;
        // move to interactable
        agent.SetDestination(targetPos);

        // Wait until we reach the interactable, and check for threats on journey
        while (currentInteractable != null &&
               Vector3.Distance(transform.position, targetPos) > agent.stoppingDistance)
        {
            if (CheckForThreats())
            {
                yield break;
            }
            else if (stateTimer <= 0)
            {
                TransitionToState(ElementalState.Idle);
            }
            stateTimer -= Time.deltaTime;
            yield return null;
        }
        // we have reached interact point
        agent.isStopped = true;

        // Perform interaction
        if (actionEffect != null)
            actionEffect.Play();

        stats.IncreaseHappiness(20);

        // Safely interact
        if (currentInteractable != null)
        {
            currentInteractable.GetComponent<ElementalObject>().Despawn();
        }

        // Clean up and return to idle
        currentInteractable = null;
        TransitionToState(ElementalState.Idle);
    }
}

