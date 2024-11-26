using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class SpiritUI : MonoBehaviour
{
    public Slider hpSlider;
    public Slider happinessSlider;
    public Slider staminaSlider;
    public TMP_Text currentState;

    private SpiritStats stats;
    private SpiritHappiness happiness;
    private SpiritBehavior SpiritBehavior;

    private void Start()
    {
        stats = GetComponent<SpiritStats>();
        happiness = GetComponent<SpiritHappiness>();
        SpiritBehavior = GetComponent<SpiritBehavior>();
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
        currentState.text = SpiritBehavior.currentState.ToString();
    }
}
