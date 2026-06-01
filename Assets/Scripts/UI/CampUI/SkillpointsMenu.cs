using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class SkillpointsMenu : MonoBehaviour
{
    public int AvailableSkillpoints;
    public TextMeshProUGUI AvailableSkillpointsMenu;
    public SkillpointSwitch[] SkillpointSwitches;
    public InvestPointsButtonController investPointsButtonController;

    private void Awake()
    {
        SkillpointSwitches = GetComponentsInChildren<SkillpointSwitch>();
    }

    private void OnEnable()
    {
        AvailableSkillpoints = CampManager.instance.playerDataController.AvailableStatPoints.Value;
        UpdateText();
    }

    public void UpdateText()
    {
        AvailableSkillpointsMenu.text = $"Skillpoints: {AvailableSkillpoints}";
    }

    public void ConfirmChanges()
    {
        UnitController controller = CampManager.instance.TrackedUnitController;
        StatAllocation statAllocation = new StatAllocation();
        statAllocation.statChanges = new StatChange[7];
        for (int i = 0; i < SkillpointSwitches.Length; i++)
        {
            statAllocation.statChanges[i] = new StatChange();
            statAllocation.statChanges[i].statType = SkillpointSwitches[i].Attribute;
            statAllocation.statChanges[i].amount = SkillpointSwitches[i].CurrentlyInvestedPoints;
        }
        controller.RequestConfirmSkillpointChangesServerRpc(controller.NetworkObjectId, statAllocation);
        ResetSkillpointSwitches();
    }

    public void ResetSkillpointSwitches()
    {
        foreach (SkillpointSwitch skillpointSwitch in SkillpointSwitches)
        {
            skillpointSwitch.Reset();
        }
    }
}

public class StatAllocation : INetworkSerializable
{
    public StatChange[] statChanges;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref statChanges);
    }
}

public class StatChange : INetworkSerializable
{
    public StatType statType;
    public int amount;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref statType);
        serializer.SerializeValue(ref amount);
    }
}
