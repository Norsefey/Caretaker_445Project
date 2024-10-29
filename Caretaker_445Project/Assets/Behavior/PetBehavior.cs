using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PetBehavior : MonoBehaviour
{
    public PetNeeds needs { get; private set; }
    private PetState currentState;
    private Dictionary<PetStateType, PetState> states;
    private List<StateTransitionRule> transitionRules;
    private Dictionary<string, Transform> pointsOfInterest;

    [SerializeField] private float urgentNeedThreshold = 30f;
    [SerializeField] private float criticalNeedThreshold = 15f;

    public Animator anime;
    void Start()
    {
        InitializeComponents();
        SetupStates();
        //SetupTransitionRules();

        // Start with idle state
        //ChangeState(PetStateType.Idle);
    }
    private void InitializeComponents()
    {
        needs = new PetNeeds();
        states = new Dictionary<PetStateType, PetState>();
        transitionRules = new List<StateTransitionRule>();
        pointsOfInterest = new Dictionary<string, Transform>();
    }

    private void SetupStates()
    {
        // Initialize all possible states
        states[PetStateType.Idle] = new IdleState(this);
        states[PetStateType.Wander] = new WanderState(this);
    }
    private void SetupTransitionRules()
    {
        // Critical needs (highest priority)
        AddTransitionRule(PetStateType.Idle, PetStateType.SeekFood, 100,
            pet => needs.GetNeed(PetNeedType.Hunger) < criticalNeedThreshold);
        AddTransitionRule(PetStateType.Wander, PetStateType.SeekFood, 100,
            pet => needs.GetNeed(PetNeedType.Hunger) < criticalNeedThreshold);

        // Urgent needs (high priority)
        AddTransitionRule(PetStateType.Idle, PetStateType.Sleep, 80,
            pet => needs.GetNeed(PetNeedType.Energy) < urgentNeedThreshold);
        AddTransitionRule(PetStateType.Wander, PetStateType.Clean, 80,
            pet => needs.GetNeed(PetNeedType.Cleanliness) < urgentNeedThreshold);

        // Normal transitions (medium priority)
        AddTransitionRule(PetStateType.Idle, PetStateType.Wander, 50,
            pet => Random.Range(0f, 100f) < 30f);
        AddTransitionRule(PetStateType.Wander, PetStateType.Idle, 50,
            pet => Random.Range(0f, 100f) < 20f);

        // Interest-based transitions (lower priority)
        AddTransitionRule(PetStateType.Idle, PetStateType.Play, 30,
            pet => IsPointOfInterestNearby("Toy") && needs.GetNeed(PetNeedType.Happiness) < 70f);
    }
    private void AddTransitionRule(PetStateType from, PetStateType to, int priority, System.Func<PetBehavior, bool> condition)
    {
        transitionRules.Add(new StateTransitionRule(from, to, priority, condition));
    }
    void Update()
    {
        needs.UpdateNeeds(Time.deltaTime);
        CheckStateTransitions();
        currentState?.UpdateState();
    }
    private void CheckStateTransitions()
    {
        var validTransitions = transitionRules
            .Where(rule => rule.fromState == currentState.StateType && rule.condition(this))
            .OrderByDescending(rule => rule.priority);

        var nextTransition = validTransitions.FirstOrDefault();
        if (nextTransition != null)
        {
            ChangeState(nextTransition.toState);
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
            Debug.Log($"Pet changed state to: {newStateType}");
        }
        else
        {
            Debug.LogError($"State {newStateType} not found!");
        }
    }
    public bool IsPointOfInterestNearby(string poiType, float checkRadius = 5f)
    {
        if (pointsOfInterest.TryGetValue(poiType, out Transform poi))
        {
            return Vector3.Distance(transform.position, poi.position) < checkRadius;
        }
        return false;
    }

    public void RegisterPointOfInterest(string name, Transform point)
    {
        pointsOfInterest[name] = point;
    }

    public Transform GetPointOfInterest(string name)
    {
        return pointsOfInterest.ContainsKey(name) ? pointsOfInterest[name] : null;
    }
}
