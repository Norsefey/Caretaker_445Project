using UnityEngine;
public abstract class ElementalState
{
    protected ElementalManager elemental;
    protected StateMachine stateMachine;
    public virtual void Enter(ElementalManager elemental, StateMachine stateMachine)
    {
        this.elemental = elemental;
        this.stateMachine = stateMachine;
    }
    public virtual void Exit() { }
    public virtual void Update() { }
    public virtual void CheckTransitions() { }
    public virtual void OnDamageTaken(ElementalManager attacker) { }
    protected bool CheckForThreats()
    {
        Collider[] nearbyElementals = Physics.OverlapSphere(
        elemental.transform.position,
            elemental.DetectionRange,
            elemental.elementalLayer
        );

        foreach (var collider in nearbyElementals)
        {
            if (collider.gameObject == elemental.gameObject) continue;

            ElementalManager otherElemental = collider.GetComponent<ElementalManager>();
            if (otherElemental != null && otherElemental.elementalData != elemental.elementalData)
            {
                if (ShouldFight(otherElemental))
                {
                    stateMachine.ChangeState(new PursuitState(otherElemental.transform));
                }
                else
                {
                   stateMachine.ChangeState(new FleeState(otherElemental.transform));
                }
                return true;
            }
        }
        return false;
    }
    protected bool CheckForInteractables()
    {
        Collider[] nearbyInteractables = Physics.OverlapSphere(
        elemental.transform.position,
            elemental.DetectionRange,
            elemental.interactableLayer
        );

        foreach (var collider in nearbyInteractables)
        {
            Interactable interactable = collider.GetComponent<Interactable>();
            if (interactable != null)
            {
                if(interactable.CanInteract(elemental, InteractionType.Contribute) && elemental.elementType == interactable.elementType)
                {
                  if(elemental.ResourceCollected.currentAmount > 0)
                    {
                        stateMachine.ChangeState(new InteractState(interactable, InteractionType.Contribute));
                        return true;
                    }
                }
                else if(!elemental.FullCarryCapacity() && elemental.ResourceCollected.resourceType == interactable.elementType && interactable.CanInteract(elemental, InteractionType.Collect))
                {
                    stateMachine.ChangeState(new InteractState(interactable, InteractionType.Collect));
                    return true;
                }
            }
        }
        return false;
    }
    protected bool ShouldFight(ElementalManager otherSpirit)
    {
        float randomness = Random.Range(0.0f, 1.0f);
        return randomness > .4f ||
               elemental.CurrentHP > otherSpirit.CurrentHP ||
               elemental.Damage > otherSpirit.Damage ||
               elemental.StaminaPercentage <= .3f;
    }
}
