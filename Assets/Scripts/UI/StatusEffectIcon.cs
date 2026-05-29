using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StatusEffectIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public StatusEffect StatusEffect;
    public Image StatusEffectImage;
    private TooltipData tooltipData;

    public void AssignStatusEffect(StatusEffect statusEffect)
    {
        StatusEffect = statusEffect;
        StatusEffectImage.sprite = statusEffect.StatusSprite;
        tooltipData = new TooltipData(StatusEffect.StatusEffectName, "", StatusEffect.StatusDescription, "");
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
