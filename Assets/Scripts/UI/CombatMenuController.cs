using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatMenuController : MonoBehaviour
{
    public UnitController CurrentUnitController;
    public UnitController TargetUnitController;

    public FightPanelController FightPanel;
    public ItemPanelController ItemPanel;
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

    private void Awake()
    {
        TurnManager.OnRefreshUI += FightPanel.SetupAbilityButtons;
        TurnManager.OnRefreshUI += ItemPanel.SetupAbilityButtons;
        TurnManager.OnRefreshUI += SetupUI;
    }

    private void OnDestroy()
    {
        TurnManager.OnRefreshUI -= FightPanel.SetupAbilityButtons;
        TurnManager.OnRefreshUI -= ItemPanel.SetupAbilityButtons;
        TurnManager.OnRefreshUI -= SetupUI;
    }

    public void ActivateFightPanel()
    {
        if (!CurrentUnitController.IsActiveTurn)
            return;

        if (!CurrentUnitController.IsOwner)
        {
            Debug.Log($"Not the owner of the current turn's unit, not enabling UI");
            return;
        }


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
        if (controller.enemyController != null || !controller.IsOwner)
        {
            gameObject.SetActive(false);
            return;
        }

        CurrentUnitController = controller;
        UpdateUI();
        controller.OnDisplayedHealthChanged += UpdateUI;
        controller.OnDisplayedManaChanged += UpdateUI;
        gameObject.SetActive(true);
    }

    public void UpdateUI()
    {
        UnitName.text = CurrentUnitController.UnitName;
        UnitLevel.text = "Lvl " + 1;
        UnitExp.text = "0/20";
        TurnTimer.text = "30s";
        UnitGold.text = "0";

        HealthBarText.text = CurrentUnitController.DisplayedHealth + "/" + CurrentUnitController.MaxHealth;
        HealthBarFill.fillAmount = (float)CurrentUnitController.DisplayedHealth / CurrentUnitController.MaxHealth;

        ManaBarText.text = CurrentUnitController.DisplayedMana + "/" + CurrentUnitController.MaxMana;
        ManaBarFill.fillAmount = (float)CurrentUnitController.DisplayedMana / CurrentUnitController.MaxMana;
    }
}
