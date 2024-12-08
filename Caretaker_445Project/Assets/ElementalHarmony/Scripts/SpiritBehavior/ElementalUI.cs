using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class ElementalUI : MonoBehaviour
{
    public Slider hpSlider;
    public Slider happinessSlider;
    public Slider staminaSlider;
    public TMP_Text currentState;

    private ElementalStats stats;
    private ElementalHappiness happiness;
    private ElementalBehavior elementalBehavior;

    private void Start()
    {
        stats = GetComponent<ElementalStats>();
        happiness = GetComponent<ElementalHappiness>();
        elementalBehavior = GetComponent<ElementalBehavior>();
        hpSlider.maxValue = stats.currentHP;
        hpSlider.value = stats.currentHP;
        happinessSlider.maxValue = happiness.maxHappiness;
        happinessSlider.value = happiness.happiness;
        staminaSlider.maxValue = stats.currentStamina;
        staminaSlider.value = stats.currentStamina;
    }

    private void Update()
    {
        hpSlider.value = stats.currentHP;
        happinessSlider.value = happiness.happiness;
        staminaSlider.value = stats.currentStamina;
        currentState.text = elementalBehavior.currentState.ToString();
    }
}
