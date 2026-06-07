using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AbilityUnlockButtonController : MonoBehaviour
{
    public Image buttonImage;
    public TextMeshProUGUI AbilityUnlockText;
    public GameObject AbilityUnlockMenu;

    public Color NoPointsColor;
    public Color AvailablePointsColor;

    public void UpdateText(int points)
    {
        AbilityUnlockText.text = $"Unlock Abilities ({points})";
        buttonImage.color = points == 0 ? NoPointsColor : AvailablePointsColor;
    }

    public void ToggleAbilityUnlockMenu()
    {
        AbilityUnlockMenu.SetActive(!AbilityUnlockMenu.activeSelf);
    }
}
