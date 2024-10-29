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
public class PetNeeds
{
    private Dictionary<PetNeedType, float> needs = new Dictionary<PetNeedType, float>();
    private Dictionary<PetNeedType, float> decayRates = new Dictionary<PetNeedType, float>();

    public PetNeeds()
    {
        foreach (PetNeedType need in System.Enum.GetValues(typeof(PetNeedType)))
        {
            needs[need] = 100f;
            decayRates[need] = 2f; // Default decay rate
        }
    }
    public float GetNeed(PetNeedType need) => needs[need];
    public void SetDecayRate(PetNeedType need, float rate) => decayRates[need] = rate;
    public void UpdateNeeds(float deltaTime)
    {
        foreach (var need in needs.Keys.ToList())
        {
            needs[need] = Mathf.Max(0, needs[need] - decayRates[need] * deltaTime);
        }
    }
    public void ModifyNeed(PetNeedType need, float amount)
    {
        needs[need] = Mathf.Clamp(needs[need] + amount, 0f, 100f);
    }
}
