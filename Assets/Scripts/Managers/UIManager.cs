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
            currentlySelectedUnit.OnManaValueChanged -= UpdateContextMenu;
            currentlySelectedUnit.OnHealthValueChanged -= UpdateContextMenu;
            currentlySelectedUnit = null;
        }
    }

    public void AssignContextMenu(UnitController controller)
    {
        if (currentlySelectedUnit != null && currentlySelectedUnit != controller)
        {
            //Unsubscribe from the old events
            currentlySelectedUnit.OnManaValueChanged -= UpdateContextMenu;
            currentlySelectedUnit.OnHealthValueChanged -= UpdateContextMenu;
        }

        currentlySelectedUnit = controller;

        currentlySelectedUnit.OnManaValueChanged += UpdateContextMenu;
        currentlySelectedUnit.OnHealthValueChanged += UpdateContextMenu;

        UpdateContextMenu();
    }

    private void UpdateContextMenu()
    {
        ContextMenuUnitName.text = currentlySelectedUnit.UnitName;

        ContextMenuHealthBarFill.fillAmount = (float)currentlySelectedUnit.CurrentHealth / currentlySelectedUnit.MaxHealth;
        ContextMenuHealthBarText.text = currentlySelectedUnit.CurrentHealth + "/" + currentlySelectedUnit.MaxHealth;

        ContextMenuManaBarFill.fillAmount = (float)currentlySelectedUnit.CurrentMana / currentlySelectedUnit.MaxMana;
        ContextMenuManaBarText.text = currentlySelectedUnit.CurrentMana + "/" + currentlySelectedUnit.MaxMana;
    }

    public void EnableCombatUI()
    {
        CombatUI.SetActive(true);
        EventUI.SetActive(false);
    }

    public void EnableEventUI()
    {
        EventUI.SetActive(true);
        CombatUI.SetActive(false);
    }
}
