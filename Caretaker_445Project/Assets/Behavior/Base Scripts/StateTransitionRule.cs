using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;


using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class NeedCondition
{
    public PetNeedType needType;
    [Range(0f, 100f)]
    public float threshold;
    public bool invertCheck; // false = below threshold, true = above threshold

    public bool EvaluateCondition(PetBehavior pet)
    {
        float currentValue = pet.GetNeedValue(needType);
        return invertCheck ?
            currentValue > threshold :
            currentValue < threshold;
    }
}

[System.Serializable]
public class StateTransitionRule
{
    public string transitionName;
    [Space(5)]
    public PetStateType fromState;
    public PetStateType toState;
    [Space(5)]
    public bool canTransitionFromAnyState = false;

    [Header("Need Conditions")]
    public List<NeedCondition> needConditions = new();
    public bool requireAllNeedConditions = true; // false = ANY condition, true = ALL conditions

    [Header("Priority Settings")]
    [Range(0, 100)]
    public int priority;

    [Header("Random Chance")]
    [Range(0f, 100f)]
    public float randomChance;

    [Header("Point of Interest")]
    public bool requiresPointOfInterest;
    public string pointOfInterestTag;

    public bool EvaluateCondition(PetBehavior pet, PetStateType currentState)
    {
        // Check if this rule can be triggered from the current state
        if (!canTransitionFromAnyState && fromState != currentState)
            return false;

        // Evaluate need conditions
        bool needsCheck = EvaluateNeedConditions(pet);

        // Check random chance
        bool randomCheck = randomChance <= 0 || Random.Range(0f, 100f) < randomChance;

        // Check point of interest
        bool poiCheck = !requiresPointOfInterest || pet.IsPointOfInterestAvailable(pointOfInterestTag);

        return needsCheck && randomCheck && poiCheck;
    }

    private bool EvaluateNeedConditions(PetBehavior pet)
    {
        if (needConditions.Count == 0)
            return true;

        if (requireAllNeedConditions)
        {
            // ALL conditions must be true
            return needConditions.All(condition => condition.EvaluateCondition(pet));
        }
        else
        {
            // ANY condition can be true
            return needConditions.Any(condition => condition.EvaluateCondition(pet));
        }
    }

    [System.Serializable]
    public class PointOfInterest
    {
        public string tag;
        public Transform location;
        [Range(0f, 10f)]
        public float interactionRadius = 1f;
    }
}
