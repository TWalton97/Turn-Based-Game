using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StatusEffectIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public StatusEffectInstance StatusEffectInstance;
    public Image StatusEffectImage;
    private TooltipData tooltipData;

    public void AssignStatusEffect(StatusEffectInstance statusEffectInstance)
    {
        StatusEffectInstance = statusEffectInstance;
        UpdateStatusEffectInfo();
    }

    void OnDisable()
    {
        TooltipManager.DisableTooltipIfSource?.Invoke(this);
    }

    public void UpdateStatusEffectInfo()
    {
        if (StatusEffectInstance == null)
            return;

        StatusEffectImage.sprite = StatusEffectInstance.StatusEffect.StatusSprite;
        tooltipData = new TooltipData(
            StatusEffectInstance.StatusEffect.StatusEffectName,
            BuildTurnsRemainingString(StatusEffectInstance),
            StatusEffectInstance.StatusEffect.ConstructDescriptionString(StatusEffectInstance),
            ""
            );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        TooltipManager.instance.EnableTooltipAtPosition(tooltipData, eventData.position + new Vector2(150, 0), this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.DisableTooltipIfSource?.Invoke(this);
    }

    public string BuildTurnsRemainingString(StatusEffectInstance instance)
    {
        if (instance.RemainingNumberOfTurns > 1)
        {
            return instance.RemainingNumberOfTurns.ToString() + " turns remaining";
        }
        return instance.RemainingNumberOfTurns.ToString() + " turn remaining";
    }
}
