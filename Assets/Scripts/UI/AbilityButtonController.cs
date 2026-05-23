using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AbilityButtonController : MonoBehaviour
{
    public CombatMenuController combatMenuController;
    public BaseAbility Ability;
    public TextMeshProUGUI AbilityName;

    private void Awake()
    {
        combatMenuController = GetComponentInParent<CombatMenuController>();
    }

    public void EnableButton(BaseAbility ability)
    {
        Ability = ability;
        AbilityName.text = ability.AbilityName;
        gameObject.SetActive(true);
    }

    public void DisableButton()
    {
        Ability = null;
        gameObject.SetActive(false);
    }

    public void ActivateButton()
    {
        if (combatMenuController.CurrentUnitController.CurrentMana < Ability.ManaCost)
            return;

        combatMenuController.ActivateTargetSelectionPanel(Ability);
    }
}
