using UnityEngine;

public class StateMachine : MonoBehaviour
{
    private ElementalState currentState;
    private ElementalManager elemental;
    private void Start()
    {
        elemental = GetComponent<ElementalManager>();
        ChangeState(new IdleState());
    }
    private void Update()
    {
        if (currentState != null)
        {
            currentState.Update();
            currentState.CheckTransitions();
        }
    }
    public void ChangeState(ElementalState newState)
    {
        if (currentState != null)
        {
            currentState.Exit();
        }

        currentState = newState;
        currentState.Enter(elemental, this);


        if (elemental.elementalUI != null)
        {
            elemental.elementalUI.UpdateStateUI(newState.ToString());
        }
    }
}
