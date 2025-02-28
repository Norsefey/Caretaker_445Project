using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class ElementalUI : MonoBehaviour
{
    public Slider hpSlider;
    public Slider staminaSlider;
    public Slider resourceSlider;
    public TMP_Text currentStateText;

    private ElementalManager stats;

    private void Start()
    {
        stats = GetComponent<ElementalManager>();
        hpSlider.maxValue = stats.CurrentHP;
        hpSlider.value = stats.CurrentHP;
        staminaSlider.maxValue = stats.CurrentStamina;
        staminaSlider.value = stats.CurrentStamina;

        resourceSlider.maxValue = stats.MaxCarryAmount;
        resourceSlider.value = stats.ResourceCollected.currentAmount;

        
    }
    private void Update()
    {
        
    }
    public void UpdateHPUI(float value)
    {
        hpSlider.value = value;
    }
    public void UpdateStaminaUI(float value)
    {
        staminaSlider.value = value;
    }
    public void UpdateCarryUI(float value)
    {
        resourceSlider.value = value;
    }
    public void UpdateStateUI(string state)
    {
        string stateName = state;

        string cleanedName = stateName.Replace("State", "");
        currentStateText.text = cleanedName;
        /*if (state.Contains("Idle"))
        {
            currentStateText.text = "Idling";
        }*/
    }
}
