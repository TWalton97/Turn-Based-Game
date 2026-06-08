using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SkillpointSwitch : MonoBehaviour
{
    public StatType Attribute;
    public int CurrentlyInvestedPoints;
    public TextMeshProUGUI CurrentlyInvestedPointsText;
    private SkillpointsMenu skillpointsMenu;

    public Color DefaultTextColor;
    public Color InvestedTextColor;

    private int amountOfPoints;

    void Awake()
    {
        skillpointsMenu = GetComponentInParent<SkillpointsMenu>();
    }

    public void InitializeSkillpointSwitch(float amountOfPoints)
    {
        this.amountOfPoints = (int)amountOfPoints;
        Reset();
    }

    public void TryAddPoint()
    {
        if (skillpointsMenu.AvailableSkillpoints > 0)
        {
            skillpointsMenu.AvailableSkillpoints -= 1;
            skillpointsMenu.UpdateText();
            CurrentlyInvestedPoints += 1;
            UpdateText();
        }
    }

    public void TryRemovePoint()
    {
        if (CurrentlyInvestedPoints > 0)
        {
            skillpointsMenu.AvailableSkillpoints += 1;
            skillpointsMenu.UpdateText();
            CurrentlyInvestedPoints -= 1;
            UpdateText();
        }
    }

    private void UpdateText()
    {
        CurrentlyInvestedPointsText.text = $"({amountOfPoints + CurrentlyInvestedPoints})";
        if (CurrentlyInvestedPoints == 0)
        {
            CurrentlyInvestedPointsText.color = DefaultTextColor;
        }
        else
        {
            CurrentlyInvestedPointsText.color = InvestedTextColor;
        }
    }

    public void Reset()
    {
        CurrentlyInvestedPoints = 0;
        CurrentlyInvestedPointsText.text = $"({amountOfPoints + CurrentlyInvestedPoints})";
        CurrentlyInvestedPointsText.color = DefaultTextColor;
    }
}
