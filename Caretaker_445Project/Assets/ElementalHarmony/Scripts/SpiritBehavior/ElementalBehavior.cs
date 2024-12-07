using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public enum ElementalState
{
    Idle,
    Roaming,
    Sleeping,
    Interacting,
    Pursuing,
    Attacking,
    Fleeing
}
public class ElementalBehavior : MonoBehaviour
{    // do to time constraints, all states are handled in this one script

    [HideInInspector] public ElementalStats stats;
    protected NavMeshAgent agent;
    protected ElementalCombat combat;
    protected ElementalData elementalData;

    [Header("State Settings")]
    public ElementalState currentState = ElementalState.Idle;
    public float idleTime = 5f;
    public float roamRadius = 10f;
    public float sleepChance = 0.1f;
    public float sleepDuration = 10f;

    [Header("VFX")]
    public ParticleSystem actionEffect;

    [Header("Combat Settings")]
    public float pursuitRange = 15f;
    public float giveUpRange = 20f;
    public LayerMask elementalLayer;
    public LayerMask interactableLayer;

    protected float stateTimer;
    protected Transform currentTarget;
    protected Transform currentAggressor;
    protected Interactable currentInteractable;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        stats = GetComponent<ElementalStats>();
        combat = GetComponent<ElementalCombat>();
        elementalData = stats.elementalData;
    }

    private void Start()
    {
        agent.speed = stats.moveSpeed;
        StartCoroutine(StateHandler());
        // update count on spawn, should only be done here
        PlayerManager.Instance.UpdateElementalCount(stats, 1);

        Instantiate(elementalData.energyOrb, transform.position + Vector3.up, Quaternion.identity);
    }
    protected virtual IEnumerator StateHandler()
    {
        while (true)
        {
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
    protected virtual IEnumerator HandleIdleState()
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

            TrySpawnObject();

            stateTimer -= Time.deltaTime;
            stats.RestoreStamina(.5f);

            yield return null;
        }
    }
    protected virtual void TrySpawnObject()
    {

    }
    protected virtual IEnumerator HandleRoamState()
    {
        Debug.Log($"{elementalData.elementalName} is roaming");
        Vector3 randomPoint = GetRandomPointInRadius(transform.position, roamRadius);
        agent.isStopped = false;
        agent.SetDestination(randomPoint);
        stats.RestoreStamina(1);

        while (Vector3.Distance(transform.position, randomPoint) > agent.stoppingDistance)
        {
            stats.IncreaseHappiness(elementalData.happinessIncreaseRate * Time.deltaTime);
            // Check for threats
            if (CheckForThreats()) yield break;

            // Check for interactables
            if (Random.value < 0.1f && CheckForInteractables()) yield break;

            yield return null;
        }

        TransitionToState(ElementalState.Idle);
    }
    protected virtual IEnumerator HandleSleepState()
    {
        Debug.Log($"{elementalData.elementalName} is sleeping");
        agent.isStopped = true;
        stateTimer = sleepDuration;

        while (stateTimer > 0)
        {
            // Can still be woken up by threats, leave the loop
            /*if (CheckForThreats()) yield break;*/
            // A natural heal rate while sleeping, which gives purpose to sleep mode
            stats.RestoreHP(2);
            stats.RestoreStamina(2);

            stateTimer -= Time.deltaTime;
            yield return null;
        }
        stats.IncreaseHappiness(10);
        TransitionToState(ElementalState.Idle);
    }
    protected virtual IEnumerator HandleInteractState()
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
            }else if(stateTimer <= 0)
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
            yield return currentInteractable.Interact(this);
        }

        // Clean up and return to idle
        currentInteractable = null;
        TransitionToState(ElementalState.Idle);
    }
    public void RemoveInteractable()
    {
        Debug.Log("Interactable Removed");
        TransitionToState(ElementalState.Idle);
        currentInteractable = null;
    }
    protected virtual IEnumerator HandlePursuitState()
    {
        if (currentTarget == null || stats.currentStamina <= 2)
        {
            TransitionToState(ElementalState.Idle);
            yield break;
        }

        Debug.Log($"{elementalData.elementalName} is pursuing {currentTarget.name}");
        agent.isStopped = false;
        while (currentTarget != null && stats.DecreaseStamina(1) && agent.isActiveAndEnabled)
        {
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

            // Give up chase if too far
            if (distanceToTarget > giveUpRange)
            {
                Debug.Log($"{elementalData.elementalName} gave up chase");
                // lose happiness if target gets away
                stats.DecreaseHappiness(5);
                TransitionToState(ElementalState.Idle);
                yield break;
            }

            // Transition to attack if in range
            if (distanceToTarget <= combat.attackRange)
            {
                TransitionToState(ElementalState.Attacking);
                yield break;
            }

            // Update pursuit
            agent.SetDestination(currentTarget.position);
            yield return null;
        }

        TransitionToState(ElementalState.Idle);
    }
    protected virtual IEnumerator HandleAttackState()
    {
        if (currentTarget == null)
        {
            TransitionToState(ElementalState.Idle);
            yield break;
        }

        while (currentTarget != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

            // Return to pursuit if target moves out of range
            if (distanceToTarget > combat.attackRange)
            {
                TransitionToState(ElementalState.Pursuing);
                yield break;
            }

            // Face target
            Vector3 directionToTarget = (currentTarget.position - transform.position).normalized;
            transform.forward = directionToTarget;

            // Attack if possible
            if (combat.AttackIntervalCheck())
            {
                combat.Attack(currentTarget.GetComponent<ElementalStats>());
            }

            yield return null;
        }

        TransitionToState(ElementalState.Idle);
    }
    protected virtual IEnumerator HandleFleeState()
    {
        if (currentAggressor == null)
        {
            TransitionToState(ElementalState.Idle);
            yield break;
        }

        Debug.Log($"{elementalData.elementalName} is fleeing from {currentAggressor.name}");
        agent.isStopped = false;
        agent.speed = stats.moveSpeed * elementalData.fleeSpeedMultiplier;
        // if we are still recovering stamina stop recovering
        while (currentAggressor != null && stats.DecreaseStamina(elementalData.fleeSpeedMultiplier))
        {
            float distanceToThreat = Vector3.Distance(transform.position, currentAggressor.position);

            // Return to idle if threat is far enough
            if (distanceToThreat > giveUpRange)
            {
                agent.speed = stats.moveSpeed;
                // give some happiness when flee is successful
                stats.IncreaseHappiness(5);
                TransitionToState(ElementalState.Idle);
                yield break;
            }

            // Calculate flee position with added randomness
            Vector3 fleeDirection = (transform.position - currentAggressor.position).normalized;
            Vector3 randomOffset = Random.insideUnitSphere * 3f; // Add random offset
            Vector3 fleePosition = transform.position + (fleeDirection * 10f) + randomOffset;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(fleePosition, out hit, 10f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            yield return null;
        }

        if(currentAggressor != null)
        {
            TransitionToState(ElementalState.Attacking);
        }
        else
        {
            agent.speed = stats.moveSpeed;
            TransitionToState(ElementalState.Idle);
        }
    }
    public bool CheckForThreats()
    {
        Collider[] nearbyElementals = Physics.OverlapSphere(transform.position, elementalData.detectionRange, elementalLayer);

        foreach (var collider in nearbyElementals)
        {
            if (collider.gameObject == gameObject) continue;

            ElementalStats otherElemental = collider.GetComponent<ElementalStats>();
            if (otherElemental != null && otherElemental.elementalData != elementalData)
            {
                if (ShouldFight(otherElemental))
                {
                    currentTarget = otherElemental.transform;
                    TransitionToState(ElementalState.Pursuing);
                }
                else
                {
                    currentAggressor = otherElemental.transform;
                    TransitionToState(ElementalState.Fleeing);
                }
                return true;
            }
        }

        return false;
    }
    protected virtual bool CheckForInteractables()
    {
        Collider[] nearbyInteractables = Physics.OverlapSphere(transform.position, elementalData.detectionRange, interactableLayer);

        foreach (var collider in nearbyInteractables)
        {
            Interactable interactable = collider.GetComponent<Interactable>();
            if (interactable != null && interactable.CanInteract(this))
            {
                currentInteractable = interactable;
                TransitionToState(ElementalState.Interacting);
                return true;
            }
        }

        return false;
    }
    protected virtual bool ShouldFight(ElementalStats otherSpirit)
    {
        // some randomness to combat
        float randomness = Random.Range(0.0f, 1.0f);

        if(randomness > .4 || stats.currentHP > otherSpirit.currentHP || stats.damage > otherSpirit.damage || stats.currentStamina / stats.maxStamina <= .3f)
        {
            return true;// Only fight if have more HP, or more attack, or Low on stamina
        }
        else
        {
            return false;
        }
         
    }
    protected void TransitionToState(ElementalState newState)
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
    private void OnDisable()
    {
        StopAllCoroutines();
    }
}
