using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatMenuController : MonoBehaviour
{
    public UnitController CurrentUnitController;
    public UnitController TargetUnitController;

    public FightPanelController FightPanel;
    public PanelController ItemPanel;
    public TargetSelectionPanelController TargetSelectionPanel;

    public TextMeshProUGUI UnitName;
    public TextMeshProUGUI UnitLevel;
    public TextMeshProUGUI UnitExp;
    public TextMeshProUGUI TurnTimer;
    public TextMeshProUGUI UnitGold;

    public TextMeshProUGUI HealthBarText;
    public Image HealthBarFill;

    public TextMeshProUGUI ManaBarText;
    public Image ManaBarFill;

    private void Start()
    {
        TurnManager.OnRefreshUI += FightPanel.SetupAbilityButtons;
        TurnManager.OnRefreshUI += SetupUI;
    }

    private void OnDestroy()
    {
        TurnManager.OnRefreshUI += FightPanel.SetupAbilityButtons;
        TurnManager.OnRefreshUI += SetupUI;
    }

    public void ActivateFightPanel()
    {
        if (!CurrentUnitController.IsActiveTurn)
            return;

        if (ItemPanel.IsPanelOpened)
            ItemPanel.ActivatePanel();

        if (TargetSelectionPanel.IsPanelOpened)
            TargetSelectionPanel.ActivatePanel();

        FightPanel.ActivatePanel();
    }

    public void ActivateItemPanel()
    {
        if (!CurrentUnitController.IsActiveTurn)
            return;

        if (FightPanel.IsPanelOpened)
            FightPanel.ActivatePanel();

        ItemPanel.ActivatePanel();
    }

    public void ActivateTargetSelectionPanel(BaseAbility ability)
    {
        if (!CurrentUnitController.IsActiveTurn)
            return;
            
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

    public void SetupUI(UnitController controller)
    {
        if (controller.enemyController != null)
        {
            gameObject.SetActive(false);
            return;
        }

        CurrentUnitController = controller;
        UpdateUI();
        controller.OnHealthValueChanged += UpdateUI;
        controller.OnManaValueChanged += UpdateUI;
        gameObject.SetActive(true);
    }

    public void UpdateUI()
    {
        UnitName.text = CurrentUnitController.UnitName;
        UnitLevel.text = "Lvl " + 1;
        UnitExp.text = "0/20";
        TurnTimer.text = "30s";
        UnitGold.text = "0";

        HealthBarText.text = CurrentUnitController.CurrentHealth + "/" + CurrentUnitController.MaxHealth;
        HealthBarFill.fillAmount = (float)CurrentUnitController.CurrentHealth / CurrentUnitController.MaxHealth;

        ManaBarText.text = CurrentUnitController.CurrentMana + "/" + CurrentUnitController.MaxMana;
        ManaBarFill.fillAmount = (float)CurrentUnitController.CurrentMana / CurrentUnitController.MaxMana;
    }
}
