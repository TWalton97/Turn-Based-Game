using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager instance;

    public GameObject TooltipParentObject;
    public TextMeshProUGUI TooltipTitleText;
    public TextMeshProUGUI TooltipCostText;
    public TextMeshProUGUI TooltipDescriptionText;
    public TextMeshProUGUI TooltipCooldownText;

    private void Awake()
    {
        if (instance == null)
            instance = this;

        DisableTooltip();
    }

    public void EnableTooltipAtPosition(TooltipData data, Vector3 position)
    {
        TooltipParentObject.transform.position = position;
        TooltipTitleText.text = data.TooltipTitle;
        TooltipCostText.text = data.TooltipCost;
        TooltipDescriptionText.text = data.TooltipDescription;
        TooltipCooldownText.text = data.TooltipCooldown;
        TooltipParentObject.SetActive(true);
    }

    public void DisableTooltip()
    {
        TooltipParentObject.SetActive(false);
    }
}

public class TooltipData
{
    public string TooltipTitle;
    public string TooltipCost;
    public string TooltipDescription;
    public string TooltipCooldown;

    public TooltipData(string _tooltipTitle, string _tooltipCost, string _tooltipDescription, string _tooltipCooldown)
    {
        TooltipTitle = _tooltipTitle;
        TooltipCost = _tooltipCost;
        TooltipDescription = _tooltipDescription;
        TooltipCooldown = _tooltipCooldown;
    }
}
