using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CampDetailsPanelController : MonoBehaviour, IPointerMoveHandler
{

    public RectTransform DetailPanelRectTransform;
    public TextMeshProUGUI DetailsTitle;
    public TextMeshProUGUI DetailsInfo;
    public TextMeshProUGUI DetailsStats;

    public void PopulateDetailsPanel(string title, string info, string stats)
    {
        DetailsTitle.text = title;
        DetailsInfo.text = AddKeywordLinks(info);
        DetailsStats.text = AddKeywordLinks(stats);

        LayoutRebuilder.ForceRebuildLayoutImmediate(DetailPanelRectTransform);
    }

    private string AddKeywordLinks(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        foreach (var kvp in StatusDatabase.StatusEffects)
        {
            string statusName = kvp.Key.ToString();

            if (!text.Contains(statusName))
                continue;

            text = text.Replace(
            statusName,
            $"<link=\"{statusName}\"><color=#66CCFF>{statusName}</color></link>");
        }
        return text;
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        bool hit =
        TryHandleHover(DetailsInfo, eventData) ||
        TryHandleHover(DetailsStats, eventData);

        if (!hit)
        {
            TooltipManager.DisableTooltipIfSource?.Invoke(this);
        }
    }

    private bool TryHandleHover(TMP_Text text, PointerEventData eventData)
    {
        if (text == null)
            return false;

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(
            text,
            eventData.position,
            null);

        if (linkIndex == -1)
            return false;

        TMP_LinkInfo linkInfo = text.textInfo.linkInfo[linkIndex];
        string keyword = linkInfo.GetLinkID();

        if (StatusDatabase.StatusEffects.TryGetValue(
            new FixedString64Bytes(keyword),
            out var status))
        {
            TooltipData tooltipData = new TooltipData(
                status.StatusEffectName,
                "",
                status.StatusDescription,
                ""
            );

            TooltipManager.instance.EnableTooltipAtPosition(
                tooltipData,
                eventData.position + new Vector2(150, 0),
                this);

            return true;
        }

        return false;
    }
}
