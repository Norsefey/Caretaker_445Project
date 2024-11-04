using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PetUI : MonoBehaviour
{
    [SerializeField] private PetBehavior pet;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private Slider energySlider;
    [SerializeField] private Slider hapinessSlider;

    private void Update()
    {
        transform.LookAt(Camera.main.transform.position * -1);

        UpdateEnergyDisplay();
        UpdateHappinessDisplay();
        UpdateStateDisplay();
    }

    public void UpdateEnergyDisplay()
    {
        energySlider.value = pet.GetNeedValue(PetNeedType.Energy);
    }
    public void UpdateHappinessDisplay()
    {
        hapinessSlider.value = pet.GetNeedValue(PetNeedType.Happiness);
    }
    public void UpdateStateDisplay()
    {
        stateText.text = pet.CurrentState.ToString();
    }
}
