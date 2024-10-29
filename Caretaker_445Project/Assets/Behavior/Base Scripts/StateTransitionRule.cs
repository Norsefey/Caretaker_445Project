using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;


[System.Serializable]
public class StateTransitionRule 
{// A cleaner way to handle transitions between states, and be able to reuse them
    public PetStateType fromState;
    public PetStateType toState;
    public int priority;
    public System.Func<PetBehavior, bool> condition;

    public StateTransitionRule(PetStateType from, PetStateType to, int priority, System.Func<PetBehavior, bool> condition)
    {
        fromState = from;
        toState = to;
        this.priority = priority;
        this.condition = condition;
    }
}
