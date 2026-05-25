using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnOrderPanelController : MonoBehaviour
{
    public TurnEntryController turnEntryControllerPrefab;
    public Transform turnOrderEntryParent;

    public Dictionary<UnitController, TurnEntryController> turnEntries = new();

    public void CreateTurnEntry(UnitController controller, float initiativeValue)
    {
        if (turnEntries.ContainsKey(controller))
            return;

        TurnEntryController turnEntry = Instantiate(turnEntryControllerPrefab, turnOrderEntryParent);
        turnEntry.AssignTrackedUnit(controller, initiativeValue);
        turnEntries.Add(controller, turnEntry);
    }

    public void SetActiveTurnEntry(UnitController controller)
    {
        foreach (TurnEntryController entry in turnEntries.Values)
        {
            entry.ToggleArrow(false);
        }

        if (turnEntries.ContainsKey(controller))
        {
            turnEntries[controller].ToggleArrow(true);
        }
    }

    public void RemoveTurnEntry(UnitController controller)
    {
        if (turnEntries.ContainsKey(controller))
        {
            Destroy(turnEntries[controller].gameObject);
            turnEntries.Remove(controller);
        }
    }
}
