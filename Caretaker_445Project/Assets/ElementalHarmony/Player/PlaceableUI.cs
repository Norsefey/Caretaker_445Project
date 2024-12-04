using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlaceableUI : MonoBehaviour
{
    public GameObject buttonPrefab;
    public Transform buttonContainer;
    public PlaceableItem[] availableItems;
    private PlayerManager playerManager;
    [SerializeField] private GameObject hotbarUI;
    [SerializeField] private TMP_Text toggleText;
    [SerializeField] private TMP_Text desciptionText;
    private bool showingDoomCountDown = false;
    [SerializeField] private GameObject doomBanner;
    [SerializeField] private TMP_Text doomTimerText;
    private bool showingUI = true;
    void Start()
    {
        playerManager = FindObjectOfType<PlayerManager>();
        CreateBuildingButtons();
    }

    void CreateBuildingButtons()
    {
        foreach (PlaceableItem item in availableItems)
        {
            GameObject buttonObj = Instantiate(buttonPrefab, buttonContainer);
            // to make it look pretty button is second child of display panel
            Button button = buttonObj.transform.GetChild(1).GetComponent<Button>();

            // Set up button image
            if (button.image != null && item.icon != null)
            {
                button.image.sprite = item.icon;
            }

            // Set up button text if it exists
            Text buttonText = buttonObj.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = $"{item.itemName}\n{item.cost} orbs";
            }

            // Add click handler
            button.onClick.AddListener(() => playerManager.SelectObject(item));
        }
    }
    public void ToggleUI()
    {
        hotbarUI.SetActive(!showingUI);
        showingUI = !showingUI;
        // short hand if else statement, if we are showing say Hide, if not say Show
        toggleText.text = showingUI ? "Hide" : "Show";
    }
    public void SetDescriptionText(PlaceableItem item)
    {
        desciptionText.text = $" <color=blue>{item.cost} Energy</color> \n";
        desciptionText.text += item.description;
    }
    public void ClearDescriptionText()
    {
        desciptionText.text = string.Empty;
    }

    public void ToggleDoomBanner(bool toggle)
    {
        if(doomBanner != null)
        {
            showingDoomCountDown = toggle;
            doomBanner.SetActive(toggle);
        }
    }
    public void ShowDoomCountDown(float timer)
    {
        doomTimerText.text = timer.ToString("F0");
    }
}
