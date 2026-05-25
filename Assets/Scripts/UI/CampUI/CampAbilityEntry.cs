using TMPro;
using UnityEngine;

public class CampAbilityEntry : MonoBehaviour
{
    //Internal Data
    public BaseAbility Ability;
    public TextMeshProUGUI AbilityName;

    public void AssignAbilityToButton(BaseAbility ability)
    {
        Ability = ability;
        AbilityName.text = ability.AbilityName;
    }
}
