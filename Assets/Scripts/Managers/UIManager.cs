using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    //Stores references to UI elements that need to be enabled/disabled
    public static UIManager instance;

    //Controls Action Menu

    //Controls Unit Context Menu

    //Controls Turn Order Menu

    //Controls Status Effect Descriptions

    //Controls Selected Ability Description


    private UnitController currentlySelectedUnit;
    public TextMeshProUGUI ContextMenuUnitName;
    public Image ContextMenuHealthBarFill;
    public TextMeshProUGUI ContextMenuHealthBarText;
    public Image ContextMenuManaBarFill;
    public TextMeshProUGUI ContextMenuManaBarText;

    public GameObject CombatUI;
    public GameObject EventUI;
    public GameObject CampUI;

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    private void Start()
    {

    }

    private void OnDestroy()
    {
        if (currentlySelectedUnit != null)
        {
            currentlySelectedUnit.OnDisplayedManaChanged -= UpdateContextMenu;
            currentlySelectedUnit.OnDisplayedHealthChanged -= UpdateContextMenu;
            currentlySelectedUnit = null;
        }
    }

    public void AssignContextMenu(UnitController controller)
    {
        if (currentlySelectedUnit != null && currentlySelectedUnit != controller)
        {
            //Unsubscribe from the old events
            currentlySelectedUnit.OnDisplayedManaChanged -= UpdateContextMenu;
            currentlySelectedUnit.OnDisplayedHealthChanged -= UpdateContextMenu;
        }

        currentlySelectedUnit = controller;

        currentlySelectedUnit.OnDisplayedManaChanged += UpdateContextMenu;
        currentlySelectedUnit.OnDisplayedHealthChanged += UpdateContextMenu;

        UpdateContextMenu();
    }

    private void UpdateContextMenu()
    {
        ContextMenuUnitName.text = currentlySelectedUnit.UnitName;

        ContextMenuHealthBarFill.fillAmount = (float)currentlySelectedUnit.DisplayedHealth / currentlySelectedUnit.MaxHealth;
        ContextMenuHealthBarText.text = currentlySelectedUnit.DisplayedHealth + "/" + currentlySelectedUnit.MaxHealth;

        ContextMenuManaBarFill.fillAmount = (float)currentlySelectedUnit.DisplayedMana / currentlySelectedUnit.MaxMana;
        ContextMenuManaBarText.text = currentlySelectedUnit.DisplayedMana + "/" + currentlySelectedUnit.MaxMana;
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
}
