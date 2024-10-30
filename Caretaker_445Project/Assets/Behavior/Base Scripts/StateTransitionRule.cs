using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;


[System.Serializable]
public class StateTransitionRule 
{// A cleaner way to handle transitions between states, and be able to reuse them
    public string transitionName;
    [Space(5)]
    public PetStateType fromState;
    public PetStateType toState;
    [Space(5)]
    public PetNeedType needType;
    [Range(0, 100)]
    public int priority;
    [Range(0f, 100f), Tooltip("Set To 0 to ignore Need")]
    public float needThreshold;
    [Tooltip("By Default Checks If Lower, Invert to check if Higher")]
    public bool invertNeedCheck;
    [Space(5)]
    [Range(0f, 100f)]
    public float randomChance;
    [Space(5)]
    public bool requiresPointOfInterest;
    public string pointOfInterestTag;

    public bool EvaluateCondition(PetBehavior pet)
    {
        bool needCheck = true;
        bool randomCheck = true;
        bool poiCheck = true;

        // Check need condition
        if (needThreshold > 0)
        {
            float currentNeedValue = pet.GetNeedValue(needType);
            needCheck = invertNeedCheck ?
                currentNeedValue > needThreshold :
                currentNeedValue < needThreshold;
        }

        // Check random chance
        if (randomChance > 0)
        {
            randomCheck = Random.Range(0f, 100f) < randomChance;
        }

        // Check point of interest
        if (requiresPointOfInterest)
        {
            poiCheck = pet.IsPointOfInterestNearby(pointOfInterestTag);
        }

        return needCheck && randomCheck && poiCheck;
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
