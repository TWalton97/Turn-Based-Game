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

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        TurnManager.OnRefreshUI += FightPanel.SetupAbilityButtons;
        TurnManager.OnRefreshUI += ItemPanel.SetupItemButtons;
        TurnManager.OnRefreshUI += ToggleAndSetupCombatUI;
        TurnManager.OnActionSelected += DisableUI;
    }

    private void OnDestroy()
    {
        TurnManager.OnRefreshUI -= FightPanel.SetupAbilityButtons;
        TurnManager.OnRefreshUI -= ItemPanel.SetupItemButtons;
        TurnManager.OnRefreshUI -= ToggleAndSetupCombatUI;
        TurnManager.OnActionSelected -= DisableUI;
    }

    public void ActivateFightPanel()
    {
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

    public void ToggleAndSetupCombatUI(UnitController controller)
    {
        if (controller.enemyController != null || !controller.IsOwner)
        {
            DisableUI();
            return;
        }

        animator.SetBool("Hide", false);
        CurrentUnitController = controller;
        UpdateUI();
        controller.OnDisplayedHealthChanged += UpdateUI;
        controller.OnDisplayedManaChanged += UpdateUI;
    }

    public void DisableUI()
    {
        CloseAllMenus();
        animator.SetBool("Hide", true);
    }

    public void UpdateUI()
    {
        UnitName.text = CurrentUnitController.UnitName;
        UnitLevel.text = "Lvl " + 1;
        UnitExp.text = "0/20";
        TurnTimer.text = "30s";
        UnitGold.text = "0";

        HealthBarText.text = CurrentUnitController.DisplayedHealth.ToString("0.0") + "/" + CurrentUnitController.MaxHealth.ToString("0.0");
        HealthBarFill.fillAmount = CurrentUnitController.DisplayedHealth / CurrentUnitController.MaxHealth;
        
        ManaBarText.text = CurrentUnitController.DisplayedMana + "/" + CurrentUnitController.MaxMana;
        ManaBarFill.fillAmount = (float)CurrentUnitController.DisplayedMana / CurrentUnitController.MaxMana;
    }
}
