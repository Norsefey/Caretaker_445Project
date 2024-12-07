using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElementalHappiness : MonoBehaviour
{
    public float happiness = 0f;
    public float maxHappiness = 100f;
    private ElementalData ElementalData;

    private void Update()
    {
        ElementalData = GetComponent<ElementalStats>().elementalData;
        happiness = Mathf.Clamp(happiness, 0, maxHappiness);
    }

    public void AdjustHappiness(float amount)
    {
        happiness += amount;
        if (happiness >= maxHappiness)
        {
            Reproduce();
            happiness = 0f; // Reset happiness
        }else if(happiness <= 0)
            happiness = 0;
    }

    private void Reproduce()
    {
        Debug.Log($"{ElementalData.elementalName} reproduces!");
        GameObject newElemental = Instantiate(ElementalData.spawnPrefab, transform.position + Vector3.up + Random.insideUnitSphere * 2f, Quaternion.identity);
        Instantiate(ElementalData.energyOrb, transform.position, Quaternion.identity);
        ElementalStats stats = newElemental.GetComponent<ElementalStats>();

        stats.elementalData = ElementalData; // Use the same data but allow variability in stats

    }
}
