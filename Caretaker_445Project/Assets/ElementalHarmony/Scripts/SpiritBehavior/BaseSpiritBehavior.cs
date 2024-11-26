using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BaseSpiritBehavior : SpiritBehavior
{
    protected virtual void Awake()
    {
        // Ensure proper setup of base components
        agent = GetComponent<NavMeshAgent>();
        stats = GetComponent<SpiritStats>();
        combat = GetComponent<SpiritCombat>();
        spiritData = stats.spiritData;
    }

    // Allow overriding of interaction logic for specific spirit types
    protected virtual IEnumerator HandleSpecializedInteraction(IInteractable interactable)
    {
        yield return interactable.Interact(gameObject);
    }
}
