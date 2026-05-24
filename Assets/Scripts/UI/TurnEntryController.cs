using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TurnEntryController : MonoBehaviour
{
    private UnitController TrackedUnit;

    public TextMeshProUGUI UnitNameText;
    public TextMeshProUGUI InitiativeValueText;
    public Image HealthBarFill;
    public GameObject ActiveEntryArrow;

    private void OnDestroy()
    {
        if (TrackedUnit == null)
            return;

        TrackedUnit.OnHealthValueChanged -= UpdateHealthBar;
    }

    public void AssignTrackedUnit(UnitController controller, int initiativeValue)
    {
        TrackedUnit = controller;

        UnitNameText.text = controller.UnitName;
        InitiativeValueText.text = initiativeValue.ToString();
        UpdateHealthBar();

        TrackedUnit.OnHealthValueChanged += UpdateHealthBar;
    }
    private void UpdateHealthBar()
    {
        if (TrackedUnit == null)
            return;

        HealthBarFill.fillAmount = (float)TrackedUnit.CurrentHealth / TrackedUnit.MaxHealth;
    }

    public void ToggleArrow(bool value)
    {
        ActiveEntryArrow.SetActive(value);
    }
}
