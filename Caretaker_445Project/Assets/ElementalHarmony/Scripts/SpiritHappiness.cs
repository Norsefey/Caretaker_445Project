using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiritHappiness : MonoBehaviour
{
    public float happiness = 0f;
    public float maxHappiness = 100f;
    private SpiritData spiritData;

    private void Update()
    {
        spiritData = GetComponent<SpiritStats>().spiritData;
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
        Debug.Log($"{spiritData.spiritName} reproduces!");
        GameObject newSpirit = Instantiate(spiritData.spawnPrefab, transform.position + Random.insideUnitSphere * 2f, Quaternion.identity);
        Instantiate(spiritData.energyOrb, transform.position, Quaternion.identity);
        // Assign SpiritData to the new spirit
        SpiritStats stats = newSpirit.GetComponent<SpiritStats>();
        PlayerManager.Instance.UpdateElementalSpiritCount(stats, 1);

        stats.spiritData = spiritData; // Use the same data but allow variability in stats

    }
}
