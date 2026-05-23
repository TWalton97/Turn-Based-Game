using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatMenuController : MonoBehaviour
{
    public UnitController CurrentUnitController;
    public UnitController TargetUnitController;

    public FightPanelController FightPanel;
    public PanelController ItemPanel;
    public TargetSelectionPanelController TargetSelectionPanel;

    private void Start()
    {
        FightPanel.SetupAbilityButtons(CurrentUnitController.Abilities);
    }

    public void ActivateFightPanel()
    {
        if (ItemPanel.IsPanelOpened)
            ItemPanel.ActivatePanel();

        if (TargetSelectionPanel.IsPanelOpened)
            TargetSelectionPanel.ActivatePanel();

        FightPanel.ActivatePanel();
    }

    public void ActivateItemPanel()
    {
        if (FightPanel.IsPanelOpened)
            FightPanel.ActivatePanel();

        ItemPanel.ActivatePanel();
    }

    public void ActivateTargetSelectionPanel(BaseAbility ability)
    {
        FightPanel.ActivatePanel();
        TargetSelectionPanel.SetupTargetButtons(ability);
        TargetSelectionPanel.ActivatePanel();
    }

    public void CloseAllMenus()
    {
        if (FightPanel.IsPanelOpened)
            FightPanel.ActivatePanel();

        if (TargetSelectionPanel.IsPanelOpened)
            TargetSelectionPanel.ActivatePanel();

        if (ItemPanel.IsPanelOpened)
            ItemPanel.ActivatePanel();
    }
}
