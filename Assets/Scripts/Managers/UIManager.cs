using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    //Stores references to UI elements that need to be enabled/disabled
    public static UIManager instance;

    public GameObject ContextMenuParent;
    private UnitController currentlySelectedUnit;
    public TextMeshProUGUI ContextMenuUnitName;
    public Image ContextMenuHealthBarFill;
    public TextMeshProUGUI ContextMenuHealthBarText;
    public Image ContextMenuManaBarFill;
    public TextMeshProUGUI ContextMenuManaBarText;
    public List<StatusEffectIcon> StatusEffectIcons;

    public GameObject CombatUI;
    public GameObject EventUI;
    public GameObject CampUI;
    public Animator TravelingUI;

    private void Awake()
    {
        if (instance == null)
            instance = this;

        ProgressionManager.OnRoomLoaded += ClearContextMenu;
    }

    private void OnDestroy()
    {
        if (currentlySelectedUnit != null)
        {
            currentlySelectedUnit.OnDisplayedManaChanged -= UpdateContextMenu;
            currentlySelectedUnit.OnDisplayedHealthChanged -= UpdateContextMenu;
            currentlySelectedUnit = null;
        }

        ProgressionManager.OnRoomLoaded -= ClearContextMenu;
    }

    public void AssignContextMenu(UnitController controller)
    {
        if (currentlySelectedUnit != null && currentlySelectedUnit != controller)
        {
            //Unsubscribe from the old events
            currentlySelectedUnit.OnDisplayedManaChanged -= UpdateContextMenu;
            currentlySelectedUnit.OnDisplayedHealthChanged -= UpdateContextMenu;
            currentlySelectedUnit.OnClientDie -= ClearContextMenu;
            currentlySelectedUnit.statusEffectController.OnStatusEffectsChanged -= RefreshStatusIcons;
        }

        currentlySelectedUnit = controller;

        foreach (StatusEffectIcon icon in StatusEffectIcons)
        {
            icon.gameObject.SetActive(false);
        }

        List<StatusEffectInstance> statusEffectInstances = currentlySelectedUnit.statusEffectController.ClientActiveStatusEffectViews;

        for (int i = 0; i < statusEffectInstances.Count; i++)
        {
            StatusEffectIcons[i].AssignStatusEffect(statusEffectInstances[i]);
            StatusEffectIcons[i].gameObject.SetActive(true);
        }

        currentlySelectedUnit.OnDisplayedManaChanged += UpdateContextMenu;
        currentlySelectedUnit.OnDisplayedHealthChanged += UpdateContextMenu;
        currentlySelectedUnit.OnClientDie += ClearContextMenu;
        currentlySelectedUnit.statusEffectController.OnStatusEffectsChanged += RefreshStatusIcons;

        UpdateContextMenu();
        ContextMenuParent.SetActive(true);
    }

    public void ClearContextMenu()
    {
        if (currentlySelectedUnit != null)
        {
            currentlySelectedUnit.OnDisplayedManaChanged -= UpdateContextMenu;
            currentlySelectedUnit.OnDisplayedHealthChanged -= UpdateContextMenu;
            currentlySelectedUnit.OnClientDie -= ClearContextMenu;
            currentlySelectedUnit.statusEffectController.OnStatusEffectsChanged -= RefreshStatusIcons;

            currentlySelectedUnit = null;
        }

        ContextMenuParent.SetActive(false);
    }

    private void UpdateContextMenu()
    {
        if (currentlySelectedUnit == null)
        {
            ClearContextMenu();
            return;
        }

        ContextMenuUnitName.text = currentlySelectedUnit.UnitName;

        ContextMenuHealthBarFill.fillAmount = currentlySelectedUnit.DisplayedHealth / currentlySelectedUnit.MaxHealth;
        ContextMenuHealthBarText.text = currentlySelectedUnit.DisplayedHealth.ToString("0.0") + "/" + currentlySelectedUnit.MaxHealth.ToString("0.0");

        ContextMenuManaBarFill.fillAmount = (float)currentlySelectedUnit.DisplayedMana / currentlySelectedUnit.MaxMana;
        ContextMenuManaBarText.text = currentlySelectedUnit.DisplayedMana + "/" + currentlySelectedUnit.MaxMana;
    }

    private void RefreshStatusIcons()
    {
        if (currentlySelectedUnit == null)
            return;

        var statuses = currentlySelectedUnit.statusEffectController.ClientActiveStatusEffectViews;

        for (int i = 0; i < StatusEffectIcons.Count; i++)
        {
            if (i < statuses.Count)
            {
                StatusEffectIcons[i].AssignStatusEffect(statuses[i]);
                StatusEffectIcons[i].gameObject.SetActive(true);
            }
            else
            {
                StatusEffectIcons[i].gameObject.SetActive(false);
            }
        }
    }

    public void EnableCombatUI()
    {
        CombatUI.SetActive(true);
        EventUI.SetActive(false);
        CampUI.SetActive(false);
    }

    public void EnableEventUI()
    {
        CombatUI.SetActive(false);
        EventUI.SetActive(true);
        CampUI.SetActive(false);
    }

    public void EnableCampUI()
    {
        CombatUI.SetActive(false);
        EventUI.SetActive(false);
        CampUI.SetActive(true);
    }

    public void EnableTravelingUI()
    {
        TravelingUI.SetTrigger("Travel");
    }
}
