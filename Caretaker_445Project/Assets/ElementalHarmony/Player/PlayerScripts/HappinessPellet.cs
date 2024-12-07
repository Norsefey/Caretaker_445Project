using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HappinessPellet : ElementalObject
{
    public string pelletAttractType; // specific pellet for specific elemental
    public float happinessGiveAmount;
    public override bool CanInteract(ElementalBehavior elemental)
    {
        ElementalStats stats = elemental.stats;
        if (stats == null) return false;

        string elementalType = stats.elementalData.elementalName;

        if(elementalType.Contains(pelletAttractType) && !beingInteracted)
        {
            beingInteracted = true;
            return true;
        }
        return false;
    }
    protected override IEnumerator InteractInternal(ElementalBehavior elemental)
    {
        beingInteracted = true;
        ElementalStats stats = elemental.stats;

        stats.IncreaseHappiness(happinessGiveAmount);
        yield return null;

        Destroy(transform.parent.gameObject);
    }
}
