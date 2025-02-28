using System.Threading.Tasks;
using UnityEngine;

public class InteractState : ElementalState
{
    private Interactable targetInteractable;
    private bool isInteracting = false;
    private InteractionType interactionType;


    // Timer variables for interaction timing
    private float interactionTimer = 0f;
    private float interactionInterval = 1.0f; // 1 second between each collection

    public InteractState(Interactable interactable, InteractionType type)
    {
        targetInteractable = interactable;
        interactionType = type;
    }

    public override void Enter(ElementalManager elemental, StateMachine stateMachine)
    {
        base.Enter(elemental, stateMachine);

        if (targetInteractable == null)
        {
            Debug.LogWarning($"{elemental.elementalData.elementalName}: Interactable is null");
            stateMachine.ChangeState(new IdleState());
            return;
        }

        // Check if the interaction is possible
        if (!targetInteractable.CanInteract(elemental, interactionType))
        {
            Debug.LogWarning($"{elemental.elementalData.elementalName}: Cannot {interactionType} with {targetInteractable}");
            stateMachine.ChangeState(new IdleState());
            return;
        }


        // Reset timer
        interactionTimer = 0f;

        // move to interactable
        elemental.Agent.isStopped = false;
        elemental.Agent.SetDestination(targetInteractable.transform.position);
        Debug.Log($"{elemental.elementalData.elementalName} is moving to {interactionType} with {targetInteractable}");
    }

    public override void Update()
    {
        if (targetInteractable == null)
        {
            stateMachine.ChangeState(new IdleState());
            return;
        }
        
        if (!isInteracting && Vector3.Distance(elemental.transform.position, targetInteractable.transform.position) <= elemental.Agent.stoppingDistance)
        {
            StartInteraction();
        }

        // Continue interaction if already started
        if (isInteracting)
        {
            // Update timer
            interactionTimer += Time.deltaTime;

            // Only interact when the timer reaches the interval
            if (interactionTimer >= interactionInterval)
            {
                interactionTimer = 0f; // Reset timer
                ContinueInteraction();

                // Play animation or effect here if needed
                PlayInteractionEffect(elemental);
            }
        }
    }
    private void StartInteraction()
    {
        Debug.Log($"{elemental.elementalData.elementalName} is starting to {interactionType} with {targetInteractable}");

        elemental.Agent.isStopped = true;
        isInteracting = true;

        // Set the elemental to face the structure
        Vector3 directionToStructure = targetInteractable.transform.position - elemental.transform.position;
        directionToStructure.y = 0; // Ignore height difference
        if (directionToStructure != Vector3.zero)
            elemental.transform.rotation = Quaternion.LookRotation(directionToStructure);
    }
    private void ContinueInteraction()
    {
        if (targetInteractable != null && targetInteractable.CanInteract(elemental, interactionType))
        {
            // Perform interaction based on the interval
            bool interactionComplete = targetInteractable.Interact(elemental, interactionType);

            // If interaction is complete, go back to idle
            if (interactionComplete)
            {
                Debug.Log($"{elemental.elementalData.elementalName} has completed {interactionType} with {targetInteractable}");
                stateMachine.ChangeState(new IdleState());
            }
        }
        else
        {
            stateMachine.ChangeState(new IdleState());
        }
    }
    private void PlayInteractionEffect(ElementalManager elemental)
    {
        // Play different effects based on interaction type
        switch (interactionType)
        {
            case InteractionType.Collect:
                //Debug.Log($"{elemental.elementalData.elementalName} performs collection action");
                // elemental.Animator.SetTrigger("Collect");
                break;

            case InteractionType.Contribute:
                //Debug.Log($"{elemental.elementalData.elementalName} performs feeding action");
                // elemental.Animator.SetTrigger("Feed");
                break;
        }
    }
    public override void CheckTransitions()
    {
        if (CheckForThreats()) return;
    }
}
