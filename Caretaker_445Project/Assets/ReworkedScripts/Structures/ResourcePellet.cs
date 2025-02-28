using UnityEngine;

public class ResourcePellet : Interactable
{
    [SerializeField] public float amountGiven;

    public override bool CanInteract(ElementalManager elemental, InteractionType interactionType)
    {
        if(elemental.elementType == elementType && interactionType == InteractionType.Collect)
            return true;
        
        return false;
    }

    public override bool Interact(ElementalManager elemental, InteractionType interactionType)
    {
        float amountToGive = Mathf.Min(amountGiven, elemental.MaxCarryAmount - elemental.ResourceCollected.currentAmount);

        elemental.AddResource(amountToGive);
        Destroy(gameObject, 1f);
        return true;
    }
}
