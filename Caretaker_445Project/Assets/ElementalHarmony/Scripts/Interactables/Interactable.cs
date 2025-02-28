using UnityEngine;

public enum InteractionType
{
    Contribute,
    Collect
}

public abstract class Interactable : MonoBehaviour
{// Allows different types of objects to be referenced and do different things when referenced
    public ElementType elementType;
    public abstract bool CanInteract(ElementalManager elemental, InteractionType interactionType);
    public abstract bool Interact(ElementalManager elemental, InteractionType interactionType);
}
