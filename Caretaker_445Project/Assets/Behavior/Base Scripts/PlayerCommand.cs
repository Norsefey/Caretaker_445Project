using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class PlayerCommand : MonoBehaviour
{
    public const string petTag = "Pet",
       groundTag = "Ground",
       bedTag = "Bed",
       bathTag = "Bath",
       foodTag = "Food",
       toyTag = "Toy";

    private PetBehavior selectedPet;
    private ObeyingState obeyingState;
    [SerializeField] private GameObject selectionIndicator;
    private void Update()
    {
        // Left click to select/deselect pet
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
                if (hit.collider.CompareTag(petTag))
                {
                    SelectPet(hit.transform.GetComponent<PetBehavior>());
                }
                else
                {
                    DeselectPet();
                }
            }
        }

        // Right click to issue commands to selected pet
        if (Input.GetMouseButtonDown(1) && selectedPet != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
                // Change to obeying state if not already in it
                if (selectedPet.CurrentState.StateType != PetStateType.Obeying)
                {
                    selectedPet.ChangeState(PetStateType.Obeying);
                }

                // Get the obeying state component
                obeyingState = selectedPet.CurrentState as ObeyingState;
                if (obeyingState != null)
                {
                    // If it's an interactive object, pass both position and object
                    if (hit.collider.CompareTag(bedTag) ||
                        hit.collider.CompareTag(foodTag) ||
                        hit.collider.CompareTag(toyTag) ||
                        hit.collider.CompareTag(bathTag))
                    {
                        Vector3 targetPos = hit.collider.transform.position;
                        Debug.Log("Going To: " + hit.collider.tag);
                        obeyingState.SetDestination(targetPos, hit.collider.gameObject);
                    }
                    // If it's just ground, only pass position
                    else if (hit.collider.CompareTag(groundTag))
                    {
                        obeyingState.SetDestination(hit.point);
                    }
                }
            }
        }
    }

    private void SelectPet(PetBehavior pet)
    {
        selectedPet = pet;
        selectionIndicator.SetActive(true);
    }

    private void DeselectPet()
    {
        selectedPet = null;
        // Remove any selection visual feedback
        selectionIndicator.SetActive(false);

    }
}
