using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AbilityUnlockButtonController : MonoBehaviour
{
    private TextMeshProUGUI AbilityUnlockText;
    public GameObject AbilityUnlockMenu;

    void Awake()
    {
        AbilityUnlockText = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void UpdateText(int points)
    {
        AbilityUnlockText.text = $"Unlock Abilities ({points})";
    }

    private void UpdateText()
    {
        UpdateText(0);
    }

    public void ToggleAbilityUnlockMenu()
    {
        AbilityUnlockMenu.SetActive(!AbilityUnlockMenu.activeSelf);
    }
}
