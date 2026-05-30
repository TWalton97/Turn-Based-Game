using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TurnEntryController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
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

        TrackedUnit.OnDisplayedHealthChanged -= UpdateHealthBar;
        TrackedUnit.OnClientDie -= DestroyTurnEntry;
    }

    public void AssignTrackedUnit(UnitController controller, float initiativeValue)
    {
        TrackedUnit = controller;

        UnitNameText.text = controller.UnitName;
        InitiativeValueText.text = initiativeValue.ToString();
        UpdateHealthBar();

        TrackedUnit.OnDisplayedHealthChanged += UpdateHealthBar;
        TrackedUnit.OnClientDie += DestroyTurnEntry;
    }

    private void DestroyTurnEntry()
    {
        Destroy(gameObject);
    }

    private void UpdateHealthBar()
    {
        if (TrackedUnit == null)
            return;

        HealthBarFill.fillAmount = (float)TrackedUnit.DisplayedHealth / TrackedUnit.MaxHealth;
    }

    public void ToggleArrow(bool value)
    {
        ActiveEntryArrow.SetActive(value);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TrackedUnit == null)
            return;

        TrackedUnit.EnableHighlight();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TrackedUnit == null)
            return;

        TrackedUnit.DisableHighlight();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (TrackedUnit == null)
            return;

        UIManager.instance.AssignContextMenu(TrackedUnit);
    }
}
