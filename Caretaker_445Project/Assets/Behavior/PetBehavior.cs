using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static StateTransitionRule;
using UnityEngine.AI;
using UnityEngine.Rendering.Universal;

public class PetBehavior : MonoBehaviour
{
    [Header("Needs Configuration")]
    [SerializeField] private List<PetNeed> needs = new();

    [Header("State Transitions")]
    [SerializeField] private List<StateTransitionRule> needsRules = new();
    [SerializeField] private List<StateTransitionRule> randomChanceRules = new();

    [Header("Points of Interest")]
    [SerializeField] private List<PointOfInterest> pointsOfInterest = new();

    [Header("General Settings")]
    [SerializeField] private float urgentNeedThreshold = 30f;
    [SerializeField] private float criticalNeedThreshold = 15f;
    [SerializeField] private bool debugMode = false;

    private Dictionary<PetStateType, PetState> states;
    private PetState currentState;
    public PetState CurrentState { get { return currentState; } }
    private NavMeshAgent agent;
    private Animator anime;
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anime = GetComponent<Animator>();

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
            //{ PetStateType.Play, new PlayingState(this) },
            //{ PetStateType.SeekFood, new EatingState(this) },
            //{ PetStateType.Sleep, new SleepingState(this) }
        };
    }
    private void Update()
    {
        UpdateNeeds();
        CheckNeedsStateTransitions();
        currentState?.UpdateState();
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
        var validTransitions = needsRules
            .Where(rule => rule.fromState == currentState.StateType && rule.EvaluateCondition(this))
            .OrderByDescending(rule => rule.priority);

        var nextTransition = validTransitions.FirstOrDefault();
        if (nextTransition != null)
        {
            if(debugMode)
                Debug.Log($"Changing state from {currentState?.StateType} to {nextTransition.toState} Due to Need Selection");

            ChangeState(nextTransition.toState);
        }
    }
    public bool CheckRandomStateTransitions()
    {
        var validTransitions = randomChanceRules
            .Where(rule => rule.fromState == currentState.StateType && rule.EvaluateCondition(this))
            .OrderByDescending(rule => rule.priority);

        var nextTransition = validTransitions.FirstOrDefault();
        if (nextTransition != null)
        {
            if (debugMode)
                Debug.Log($"Changing state from {currentState?.StateType} to {nextTransition.toState} Due to Random Selection");

            ChangeState(nextTransition.toState);
            return true;
        }
        else
        {
            return false;
        }
    }
    private void ChangeState(PetStateType newStateType)
    {
        if (currentState?.StateType == newStateType)
            return;

        currentState?.ExitState();

        if (states.TryGetValue(newStateType, out PetState newState))
        {
            currentState = newState;
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
        var poi = pointsOfInterest.Find(p => p.tag == tag);
        if (poi?.location != null)
        {
            return Vector3.Distance(transform.position, poi.location.position) < poi.interactionRadius;
        }
        return false;
    }

    public Transform GetPointOfInterest(string tag)
    {
        return pointsOfInterest.Find(p => p.tag == tag)?.location;
    }

    // Get components
    public NavMeshAgent GetAgent() => agent;
    public Animator GetAnimator() => anime;
}
