using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public enum SpiritState
{
    Idle,
    Roaming,
    Sleeping,
    Interacting,
    Pursuing,
    Attacking,
    Fleeing
}
public class SpiritBehavior : MonoBehaviour
{
    [Header("State Settings")]
    public SpiritState currentState = SpiritState.Idle;
    public float idleTime = 5f;
    public float roamRadius = 10f;
    public float sleepChance = 0.1f;
    public float sleepDuration = 10f;

    [Header("Combat Settings")]
    public float pursuitRange = 15f;
    public float giveUpRange = 20f;
    public LayerMask spiritLayer;
    public LayerMask interactableLayer;

    protected NavMeshAgent agent;
    protected SpiritStats stats;
    protected SpiritCombat combat;
    protected SpiritData spiritData;

    protected float stateTimer;
    protected Transform currentTarget;
    protected Transform fleeTarget;
    protected IInteractable currentInteractable;

    private void Start()
    {
        agent.speed = stats.moveSpeed;
        StartCoroutine(StateMachine());

        Instantiate(spiritData.energyOrb, transform.position, Quaternion.identity);
    }
    protected virtual IEnumerator StateMachine()
    {
        while (true)
        {
            switch (currentState)
            {
                case SpiritState.Idle:
                    yield return HandleIdleState();
                    break;

                case SpiritState.Roaming:
                    yield return HandleRoamState();
                    break;

                case SpiritState.Sleeping:
                    yield return HandleSleepState();
                    break;

                case SpiritState.Interacting:
                    yield return HandleInteractState();
                    break;

                case SpiritState.Pursuing:
                    yield return HandlePursuitState();
                    break;

                case SpiritState.Attacking:
                    yield return HandleAttackState();
                    break;

                case SpiritState.Fleeing:
                    yield return HandleFleeState();
                    break;
            }
            yield return null;
        }
    }
    protected virtual IEnumerator HandleIdleState()
    {
        Debug.Log($"{spiritData.spiritName} is idle");
        agent.isStopped = true;
        stateTimer = idleTime;

        while (stateTimer > 0)
        {
            // Check for threats
            if (CheckForThreats()) yield break;

            // Check for interactables
            if (Random.value < 0.1f && CheckForInteractables()) yield break;

            stateTimer -= Time.deltaTime;
            yield return null;
        }

        // Transition to next state
        if (Random.value < sleepChance)
            TransitionToState(SpiritState.Sleeping);
        else
            TransitionToState(SpiritState.Roaming);
    }
    protected virtual IEnumerator HandleRoamState()
    {
        Debug.Log($"{spiritData.spiritName} is roaming");
        Vector3 randomPoint = GetRandomPointInRadius(transform.position, roamRadius);
        agent.isStopped = false;
        agent.SetDestination(randomPoint);

        while (Vector3.Distance(transform.position, randomPoint) > agent.stoppingDistance)
        {
            stats.IncreaseHappiness(spiritData.happinessIncreaseRate * Time.deltaTime);
            // Check for threats
            if (CheckForThreats()) yield break;

            // Check for interactables
            if (Random.value < 0.1f && CheckForInteractables()) yield break;

            yield return null;
        }

        TransitionToState(SpiritState.Idle);
    }
    protected virtual IEnumerator HandleSleepState()
    {
        Debug.Log($"{spiritData.spiritName} is sleeping");
        agent.isStopped = true;
        stateTimer = sleepDuration;

        while (stateTimer > 0)
        {
            // Can still be woken up by threats, leave the loop
            if (CheckForThreats()) yield break;
            // A natural heal rate while sleeping, which gives purpose to sleep mode
            StartCoroutine(stats.RestoreHP());

            stateTimer -= Time.deltaTime;
            yield return null;
        }

        StopCoroutine(stats.RestoreHP());
        stats.IncreaseHappiness(10);
        TransitionToState(SpiritState.Idle);
    }
    protected virtual IEnumerator HandleInteractState()
    {
        if (currentInteractable == null)
        {
            TransitionToState(SpiritState.Idle);
            yield break;
        }

        Debug.Log($"{spiritData.spiritName} is interacting with {currentInteractable}");
        agent.SetDestination(currentInteractable.GetInteractionPoint());
        agent.isStopped = false;
        // in case a interactable spawns while in idle or a non moving state
        agent.isStopped = false;
        // Wait until we reach the interactable
        while (currentInteractable != null &&
               Vector3.Distance(transform.position, currentInteractable.GetInteractionPoint()) > agent.stoppingDistance)
        {
            if (CheckForThreats())
            {
                yield break;
            }
            yield return null;
        }
        // Check if interactable still exists
        if (currentInteractable == null)
        {
            TransitionToState(SpiritState.Idle);
            yield break;
        }


        // Stop moving once we reach interactable
        agent.isStopped = true;
        Debug.Log($"Interacting with {currentInteractable}");

        // Store the current interactable in case it gets nulled during interaction
        IInteractable interactableRef = currentInteractable;

        // Start a coroutine to monitor the interaction
        StartCoroutine(MonitorInteraction(interactableRef));

        // Perform the interaction
        if (interactableRef != null)
        {
            yield return interactableRef.Interact(gameObject);
        }

        // Clean up and return to idle
        currentInteractable = null;
        TransitionToState(SpiritState.Idle);
    }
    private IEnumerator MonitorInteraction(IInteractable interactable)
    {
        while (currentState == SpiritState.Interacting)
        {
            // If the interactable is destroyed during interaction
            if (interactable == null || !interactable.Equals(currentInteractable))
            {
                Debug.LogWarning($"{spiritData.spiritName}: Interactable was destroyed during interaction");
                currentInteractable = null;
                TransitionToState(SpiritState.Idle);
                yield break;
            }
            yield return null;
        }
    }
    public void HandleInteractableDestroyed()
    {
        if (currentState == SpiritState.Interacting)
        {
            currentInteractable = null;
            TransitionToState(SpiritState.Idle);
        }
    }
    protected virtual IEnumerator HandlePursuitState()
    {
        if (currentTarget == null)
        {
            TransitionToState(SpiritState.Idle);
            yield break;
        }

        Debug.Log($"{spiritData.spiritName} is pursuing {currentTarget.name}");
        agent.isStopped = false;
        StopCoroutine(stats.RestoreStamina());

        while (currentTarget != null && stats.DecreaseStamina())
        {
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

            // Give up chase if too far
            if (distanceToTarget > giveUpRange)
            {
                Debug.Log($"{spiritData.spiritName} gave up chase");
                TransitionToState(SpiritState.Idle);
                yield break;
            }

            // Transition to attack if in range
            if (distanceToTarget <= combat.attackRange)
            {
                TransitionToState(SpiritState.Attacking);
                yield break;
            }

            // Update pursuit
            agent.SetDestination(currentTarget.position);
            yield return null;
        }

        TransitionToState(SpiritState.Idle);
    }
    protected virtual IEnumerator HandleAttackState()
    {
        if (currentTarget == null)
        {
            TransitionToState(SpiritState.Idle);
            yield break;
        }

        while (currentTarget != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

            // Return to pursuit if target moves out of range
            if (distanceToTarget > combat.attackRange)
            {
                TransitionToState(SpiritState.Pursuing);
                yield break;
            }

            // Face target
            Vector3 directionToTarget = (currentTarget.position - transform.position).normalized;
            transform.forward = directionToTarget;

            // Attack if possible
            if (combat.CanAttack())
            {
                combat.Attack(currentTarget.GetComponent<SpiritStats>());
            }

            yield return null;
        }

        TransitionToState(SpiritState.Idle);
    }
    protected virtual IEnumerator HandleFleeState()
    {
        if (fleeTarget == null)
        {
            TransitionToState(SpiritState.Idle);
            yield break;
        }

        Debug.Log($"{spiritData.spiritName} is fleeing from {fleeTarget.name}");
        agent.isStopped = false;
        agent.speed = stats.moveSpeed * spiritData.fleeSpeedMultiplier;
        // if we are still recovering stamina stop recovering
        StopCoroutine(stats.RestoreStamina());
        while (fleeTarget != null && stats.DecreaseStamina())
        {
            float distanceToThreat = Vector3.Distance(transform.position, fleeTarget.position);

            // Return to idle if threat is far enough
            if (distanceToThreat > giveUpRange)
            {
                agent.speed = stats.moveSpeed;
                // give some happiness when flee is successful
                stats.IncreaseHappiness(5);
                TransitionToState(SpiritState.Idle);
                yield break;
            }

            // Calculate flee position
            Vector3 fleeDirection = (transform.position - fleeTarget.position).normalized;
            Vector3 fleePosition = transform.position + fleeDirection * 10f;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(fleePosition, out hit, 10f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            yield return null;
        }

        agent.speed = stats.moveSpeed;
        StartCoroutine(stats.RestoreStamina());
        TransitionToState(SpiritState.Idle);
    }
    protected bool CheckForThreats()
    {
        Collider[] nearbySpirits = Physics.OverlapSphere(transform.position, spiritData.detectionRange, spiritLayer);

        foreach (var collider in nearbySpirits)
        {
            if (collider.gameObject == gameObject) continue;

            SpiritStats otherSpirit = collider.GetComponent<SpiritStats>();
            if (otherSpirit != null && otherSpirit.spiritData != spiritData)
            {
                if (ShouldFight(otherSpirit))
                {
                    currentTarget = otherSpirit.transform;
                    TransitionToState(SpiritState.Pursuing);
                }
                else
                {
                    fleeTarget = otherSpirit.transform;
                    TransitionToState(SpiritState.Fleeing);
                }
                return true;
            }
        }

        return false;
    }
    protected virtual bool CheckForInteractables()
    {
        Collider[] nearbyInteractables = Physics.OverlapSphere(transform.position, spiritData.detectionRange, interactableLayer);

        foreach (var collider in nearbyInteractables)
        {
            IInteractable interactable = collider.GetComponent<IInteractable>();
            if (interactable != null && interactable.CanInteract(gameObject))
            {
                currentInteractable = interactable;
                TransitionToState(SpiritState.Interacting);
                return true;
            }
        }

        return false;
    }
    protected virtual bool ShouldFight(SpiritStats otherSpirit)
    {
        // Example decision making - can be made more complex
        return stats.currentHP > otherSpirit.currentHP * 1.2f; // Only fight if significantly stronger
    }
    protected void TransitionToState(SpiritState newState)
    {
        currentState = newState;
        stateTimer = 0f;
    }
    private Vector3 GetRandomPointInRadius(Vector3 center, float radius)
    {
        Vector3 randomPoint = center + Random.insideUnitSphere * radius;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, radius, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return center;
    }
}
