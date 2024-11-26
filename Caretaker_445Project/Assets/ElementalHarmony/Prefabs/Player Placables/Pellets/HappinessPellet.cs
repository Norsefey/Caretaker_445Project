using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HappinessPellet : ElementalObject
{
    public string pelletAttractType;
    public float happinessGiveAmount;
    public override bool CanInteract(GameObject spirit)
    {
        SpiritStats stats = spirit.GetComponent<SpiritStats>();
        if (stats == null) return false;

        string spiritType = stats.spiritData.spiritName;

        if(spiritType.Contains(pelletAttractType) && !beingInteracted)
        {
            beingInteracted = true;
            return true;
        }
        return false;
    }
    protected override IEnumerator InteractInternal(GameObject spirit)
    {
        beingInteracted = true;
        SpiritStats stats = spirit.GetComponent<SpiritStats>();

        stats.IncreaseHappiness(happinessGiveAmount);
        yield return null;

        Destroy(transform.parent.gameObject);
    }
}
