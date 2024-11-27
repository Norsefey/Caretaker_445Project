using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractable
{
    bool CanInteract(GameObject spirit);
    Vector3 GetInteractionPoint();
    IEnumerator Interact(GameObject spirit);

}
