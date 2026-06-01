using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager instance;

    public RectTransform TooltipParentObject;
    public TextMeshProUGUI TooltipTitleText;
    public TextMeshProUGUI TooltipCostText;
    public TextMeshProUGUI TooltipDescriptionText;
    public TextMeshProUGUI TooltipCooldownText;

    public MonoBehaviour CurrentTooltipSource;
    public static Action<MonoBehaviour> DisableTooltipIfSource;

    private void Awake()
    {
        if (instance == null)
            instance = this;

        DisableTooltip();

        DisableTooltipIfSource += DisableTooltip;
    }

    void OnDestroy()
    {
        DisableTooltipIfSource -= DisableTooltip;
    }

    public void EnableTooltipAtPosition(TooltipData data, Vector3 position, MonoBehaviour source)
    {
        CurrentTooltipSource = source;
        TooltipParentObject.transform.position = position;
        TooltipTitleText.text = data.TooltipTitle;
        TooltipCostText.text = data.TooltipCost;
        TooltipDescriptionText.text = data.TooltipDescription;
        TooltipCooldownText.text = data.TooltipCooldown;
        TooltipParentObject.gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(TooltipParentObject);
    }

    public void DisableTooltip()
    {
        TooltipParentObject.gameObject.SetActive(false);
    }

    private void DisableTooltip(MonoBehaviour source)
    {
        if (CurrentTooltipSource == source)
        {
            DisableTooltip();
            source = null;
        }
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
