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
    [SerializeField] private Slider hungerSlider;
    [SerializeField] private Slider cleanlinessSlider;

    private void Start()
    {
        energySlider.value = 100;
        hapinessSlider.value = 100;
        hungerSlider.value = 100;
        cleanlinessSlider.value = 100;
    }

    private void Update()
    {
        Vector3 directionToCam = transform.position - Camera.main.transform.position;
        transform.rotation = Quaternion.LookRotation(directionToCam);


        UpdateEnergyDisplay();
        UpdateHappinessDisplay();
        UpdateHungerDisplay();
        UpdateCleanlinessDisplay();
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
    public void UpdateHungerDisplay()
    {
        hungerSlider.value = pet.GetNeedValue(PetNeedType.Hunger);
    }
    public void UpdateCleanlinessDisplay()
    {
        cleanlinessSlider.value = pet.GetNeedValue(PetNeedType.Cleanliness);
    }
    public void UpdateStateDisplay()
    {
        stateText.text = pet.CurrentState.ToString();
    }
}
