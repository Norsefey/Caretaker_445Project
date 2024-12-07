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
    [Header("VFX")]
    public ParticleSystem actionEffect;
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
        PlayerManager.Instance.UpdateElementalSpiritCount(stats, 1);

        Instantiate(spiritData.energyOrb, transform.position + Vector3.up, Quaternion.identity);
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
                        yield return HandleInteractState(currentInteractable.GetInteractionPoint());
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
            StartCoroutine(stats.RestoreStamina(1));

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
        StopCoroutine(stats.RestoreStamina(1));

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
            StartCoroutine(stats.RestoreStamina(2));

            stateTimer -= Time.deltaTime;
            yield return null;
        }

        StopCoroutine(stats.RestoreHP());
        stats.IncreaseHappiness(10);
        TransitionToState(SpiritState.Idle);
    }
    protected virtual IEnumerator HandleInteractState(Vector3 targetPos)
    {
        if (currentInteractable == null)
        {
            Debug.LogWarning($"{spiritData.spiritName}: Interactable became null before getting interaction point");

            TransitionToState(SpiritState.Idle);
            yield break;
        }
        Debug.Log($"{spiritData.spiritName} is interacting with {currentInteractable}");
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
                TransitionToState(SpiritState.Idle);
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
            yield return currentInteractable.Interact(gameObject);
        }

        // Clean up and return to idle
        currentInteractable = null;
        TransitionToState(SpiritState.Idle);
    }

    public void RemoveInteractable()
    {
        Debug.Log("Interactable Removed");
        TransitionToState(SpiritState.Idle);
        currentInteractable = null;
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
        StopCoroutine(stats.RestoreStamina(1));

        while (currentTarget != null && stats.DecreaseStamina() && agent.isActiveAndEnabled)
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
        StopCoroutine(stats.RestoreStamina(1));
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
        // some randomness to combat
        float randomness = Random.Range(0.0f, 1.0f);

        if(randomness > .4 || stats.currentHP > otherSpirit.currentHP || stats.damage > otherSpirit.damage || stats.currentStamina < 10)
        {
            return true;// Only fight if have more HP, or stronger, or out of stamina
        }
        else
        {
            return false;
        }
         
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
    private void OnDisable()
    {
        agent.enabled = false;
        StopAllCoroutines();
    }
}
