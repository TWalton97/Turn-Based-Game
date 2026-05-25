using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SkillpointSwitch : MonoBehaviour
{
    public Attribute Attribute;
    public int CurrentlyInvestedPoints;
    public TextMeshProUGUI CurrentlyInvestedPointsText;
    private SkillpointsMenu skillpointsMenu;

    void Awake()
    {
        skillpointsMenu = GetComponentInParent<SkillpointsMenu>();
    }

    public void TryAddPoint()
    {
        if (skillpointsMenu.AvailableSkillpoints > 0)
        {
            skillpointsMenu.AvailableSkillpoints -= 1;
            skillpointsMenu.UpdateText();
            CurrentlyInvestedPoints += 1;
            CurrentlyInvestedPointsText.text = $"({CurrentlyInvestedPoints})";
        }
    }

    public void TryRemovePoint()
    {
        if (CurrentlyInvestedPoints > 0)
        {
            skillpointsMenu.AvailableSkillpoints += 1;
            skillpointsMenu.UpdateText();
            CurrentlyInvestedPoints -= 1;
            CurrentlyInvestedPointsText.text = $"({CurrentlyInvestedPoints})";
        }
    }

    public void Reset()
    {
        CurrentlyInvestedPoints = 0;
        CurrentlyInvestedPointsText.text = $"({CurrentlyInvestedPoints})";
    }
}
