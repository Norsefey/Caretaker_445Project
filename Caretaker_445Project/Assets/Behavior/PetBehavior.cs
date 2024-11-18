using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static StateTransitionRule;
using UnityEngine.AI;
public class PetBehavior : MonoBehaviour
{
    [Header("Needs Configuration")]
    [SerializeField] private List<PetNeed> needs = new();

    [Header("State Transitions")]
    [SerializeField] private List<StateTransitionRule> globalPriorityRules = new();  // Highest priority, can trigger from any state
    [SerializeField] private List<StateTransitionRule> needsRules = new();  // Need-based transitions
    [SerializeField] private List<StateTransitionRule> randomChanceRules = new();  // Random transitions


    [Header("Points of Interest")]
    [SerializeField] private List<PointOfInterest> pointsOfInterest = new();

    [Header("General Settings")]
    [SerializeField] private float urgentNeedThreshold = 30f;
    //[SerializeField] private float criticalNeedThreshold = 15f;
    [SerializeField] private bool debugMode = false;

    [Header("State Settings")]
    //[SerializeField] private float minDistanceToConsiderAtPOI = 0.5f;
    [SerializeField] private float maxTimeInOneState = 30f;  // Prevent getting stuck in states

    private Dictionary<PetStateType, PetState> states;
    private PetState currentState;
    private float currentStateTimer = 0f;
    public PetState CurrentState { get { return currentState; } }
    private NavMeshAgent agent;
    [SerializeField]private Animator anime;
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        // Initialize default needs if none are set
        if (needs.Count == 0)
        {
            foreach (PetNeedType needType in System.Enum.GetValues(typeof(PetNeedType)))
            {
                needs.Add(new PetNeed { type = needType });
            }
        }
    }
    private void Start()
    {
        InitializeStates();
        ChangeState(PetStateType.Idle);
    }
    private void InitializeStates()
    {
        states = new Dictionary<PetStateType, PetState>
        {
            { PetStateType.Idle, new IdleState(this) },
            { PetStateType.Wander, new WanderState(this) },
            { PetStateType.Play, new PlayState(this) },
            { PetStateType.SeekFood, new FeedingState(this) },
            { PetStateType.Sleep, new SleepState(this) },
            { PetStateType.Clean, new CleanState(this) },
            { PetStateType.Interact, new InteractState(this) },
            { PetStateType.Obeying, new ObeyingState(this) }

        };
    }
    private void Update()
    {
        UpdateNeeds();
        UpdateStateTimer();
        CheckNeedsStateTransitions();
        currentState?.UpdateState();
    }
    private void UpdateStateTimer()
    {
        currentStateTimer += Time.deltaTime;
        if (currentStateTimer >= maxTimeInOneState)
        {
            if (debugMode)
                Debug.Log($"Force state change due to timeout: {currentState?.StateType}");

            ForceStateChange();
        }
    }
    private void ForceStateChange()
    {
        // First try to handle the most urgent need
        PetNeed mostUrgentNeed = needs
            .OrderBy(n => n.currentValue)
            .FirstOrDefault();

        if (mostUrgentNeed != null && mostUrgentNeed.currentValue < urgentNeedThreshold)
        {
            // Find appropriate state for the urgent need
            var appropriateState = GetAppropriateStateForNeed(mostUrgentNeed.type);
            if (appropriateState != currentState?.StateType)
            {
                ChangeState(appropriateState);
                return;
            }
        }

        // If no urgent needs, fall back to idle
        ChangeState(PetStateType.Idle);
    }
    private PetStateType GetAppropriateStateForNeed(PetNeedType needType)
    {
        switch (needType)
        {
            case PetNeedType.Hunger:
                return PetStateType.SeekFood;
            case PetNeedType.Energy:
                return PetStateType.Sleep;
            case PetNeedType.Cleanliness:
                return PetStateType.Clean;
            case PetNeedType.Happiness:
                return PetStateType.Play;
            default:
                return PetStateType.Idle;
        }
    }
    private void UpdateNeeds()
    {
        foreach (var need in needs)
        {
            need.DecayNeed(Time.deltaTime);
        }
    }
    private void CheckNeedsStateTransitions()
    {
        // First check global priority rules (like critical needs)
        var globalTransition = globalPriorityRules
            .Where(rule => rule.EvaluateCondition(this, currentState.StateType))
            .OrderByDescending(rule => rule.priority)
            .FirstOrDefault();
        // we have a transition, and isnt transiting to itself
        if (globalTransition != null && currentState.StateType != globalTransition.toState)
        {
            if (debugMode)
                Debug.Log($"Changing state from {currentState?.StateType} to {globalTransition.toState} Due to Global Priority Rule: {globalTransition.transitionName}");

            ChangeState(globalTransition.toState);
            return;
        }

        // Then check regular need-based rules
        var needTransition = needsRules
            .Where(rule => rule.EvaluateCondition(this, currentState.StateType))
            .OrderByDescending(rule => rule.priority)
            .FirstOrDefault();

        if (needTransition != null && currentState.StateType != needTransition.toState)
        {
            if (debugMode)
                Debug.Log($"Changing state from {currentState?.StateType} to {needTransition.toState} Due to Need Rule: {needTransition.transitionName}");

            ChangeState(needTransition.toState);
        }
    }
    public bool CheckRandomStateTransitions()
    {
        var randomTransition = randomChanceRules
            .Where(rule => rule.EvaluateCondition(this, currentState.StateType))
            .OrderByDescending(rule => rule.priority)
            .FirstOrDefault();

        if (randomTransition != null && currentState.StateType != randomTransition.toState)
        {
            if (debugMode)
                Debug.Log($"Changing state from {currentState?.StateType} to {randomTransition.toState} Due to Random Rule: {randomTransition.transitionName}");

            ChangeState(randomTransition.toState);
            return true;
        }

        return false;
    }
    // Helper method to check if POI exists and is accessible
    public bool IsPointOfInterestAvailable(string tag)
    {
        var poi = GetNearestPointOfInterest(tag);
        if (poi?.location == null) return false;

        // Check if there's a valid path to the POI
        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(poi.location.position, path);
        return path.status == NavMeshPathStatus.PathComplete;
    }
    public void ChangeState(PetStateType newStateType)
    {
        if (currentState?.StateType == newStateType)
            return;

        currentState?.ExitState();

        if (states.TryGetValue(newStateType, out PetState newState))
        {
            currentState = newState;
            currentStateTimer = 0f;
            currentState.EnterState();
        }
    }
    public float GetNeedValue(PetNeedType needType)
    {
        var need = needs.Find(n => n.type == needType);
        return need?.currentValue ?? 100f;
    }
    public void ModifyNeed(PetNeedType needType, float amount)
    {
        var need = needs.Find(n => n.type == needType);
        need?.ReplenishNeed(amount, Time.deltaTime);
    }
    public bool IsPointOfInterestNearby(string tag)
    {
        var poi = GetNearestPointOfInterest(tag);
        if (poi?.location != null)
        {
            return Vector3.Distance(transform.position, poi.location.position) <= poi.interactionRadius;
        }
        return false;
    }
    public PointOfInterest GetNearestPointOfInterest(string tag)
    {
        var validPOIs = pointsOfInterest.Where(p => p.tag == tag && p.location != null && p.active).ToList();

        if (!validPOIs.Any())
            return null;

        return validPOIs
            .OrderBy(p => Vector3.Distance(transform.position, p.location.position))
            .FirstOrDefault();
    }

    // Get components
    public NavMeshAgent GetAgent() => agent;
    public Animator GetAnimator() => anime;

    // Debug methods
    public void OnDrawGizmos()
    {
        if (!debugMode) return;

        // Draw interaction radius for points of interest
        foreach (var poi in pointsOfInterest)
        {
            if (poi.location != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(poi.location.position, poi.interactionRadius);
            }
        }

        // Draw current destination if agent is active
        if (agent != null && agent.hasPath)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, agent.destination);
        }
    }
}
