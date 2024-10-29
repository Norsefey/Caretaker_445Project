using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum PetStateType
{
    Idle,
    Wander,
    SeekFood,
    Sleep,
    Clean,
    Play,
    Interact
}

public abstract class PetState
{
    protected PetBehavior pet;
    protected NavMeshAgent agent;
    protected PetStateType stateType;

    // Getter
    public PetStateType StateType => stateType;

    public PetState(PetBehavior pet, PetStateType type)
    {
        this.pet = pet;
        this.agent = pet.GetComponent<NavMeshAgent>();
        this.stateType = type;
    }

    public abstract void EnterState();
    public abstract void UpdateState();
    public abstract void ExitState();
}
