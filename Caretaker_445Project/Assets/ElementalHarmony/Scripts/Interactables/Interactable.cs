using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Interactable : MonoBehaviour
{// Allows different types of objects to be referenced and do different things when referenced
    public abstract bool CanInteract(ElementalBehavior elemental);
    public abstract IEnumerator Interact(ElementalBehavior elemental);
}
