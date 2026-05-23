using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleSlot : MonoBehaviour
{
    public Team Team;
    public UnitController UnitController;
    public WorldSpaceUnitUI worldSpaceUI;
    public Transform UnitHolder;

    public void BindUnitToSlot(UnitController controller)
    {
        UnitController = controller;

        worldSpaceUI.UnitName.text = UnitController.UnitName;
        worldSpaceUI.HealthBarText.text = UnitController.CurrentHealth + "/" + UnitController.MaxHealth;
        worldSpaceUI.HealthBarFill.fillAmount = (float)UnitController.CurrentHealth / UnitController.MaxHealth;
        worldSpaceUI.ManaBarText.text = UnitController.CurrentMana + "/" + UnitController.MaxMana;
        worldSpaceUI.ManaBarFill.fillAmount = (float)UnitController.CurrentMana / UnitController.MaxMana;

        worldSpaceUI.gameObject.SetActive(true);
    }

    public void UnbindUnit()
    {
        UnitController = null;
        worldSpaceUI.gameObject.SetActive(false);
    }

}
