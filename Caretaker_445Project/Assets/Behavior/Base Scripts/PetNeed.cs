using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public enum PetNeedType
{// The types of needs the Pet will have
    Hunger,
    Energy,
    Cleanliness,
    Happiness
}
[System.Serializable]
public class PetNeed
{
    public string needName;
    public PetNeedType type;
    [Range(0f, 100f)]
    public float currentValue = 100f;
    [Range(0f, 10f)]
    public float decayRate = 2f;
    public void DecayNeed(float deltaTime)
    {
        currentValue = Mathf.Max(0f, currentValue - (decayRate * deltaTime));
    }

    public void ReplenishNeed(float modifier, float deltaTime)
    {
        // Since decay rate is constantly running, when modifying, take the set decay rate into account
        currentValue = Mathf.Clamp(currentValue + ((decayRate + modifier) * deltaTime), 0f, 100f);
    }

    public void NeedInjection(float amount)
    {
        // for a one time injection of amount into need
        currentValue = Mathf.Clamp(currentValue + amount, 0, 100);
    }
}
