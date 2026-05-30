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

    public void UpdateStatusEffectInfo()
    {
        if (StatusEffectInstance == null)
            return;

        StatusEffectImage.sprite = StatusEffectInstance.StatusEffect.StatusSprite;
        tooltipData = new TooltipData(StatusEffectInstance.StatusEffect.StatusEffectName, StatusEffectInstance.RemainingNumberOfTurns.ToString() + " turns remaining", StatusEffectInstance.StatusEffect.StatusDescription, "");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        TooltipManager.instance.EnableTooltipAtPosition(tooltipData, eventData.position + new Vector2(150, 0));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.instance.DisableTooltip();
    }
}
