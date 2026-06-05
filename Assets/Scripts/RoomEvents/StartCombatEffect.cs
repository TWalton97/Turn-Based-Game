using UnityEngine;

[CreateAssetMenu(fileName = "Events", menuName = "Events/EventEffect/CombatEffect")]
public class StartCombatEffect : EventEffect
{
    public RoomData roomData;

    public override void Execute()
    {
        RuntimeRoomData runtimeRoomData = CombatRoomManager.instance.ReturnRoomDataAsRuntime(roomData);
        ProgressionManager.instance.ForceLoadCombatRoom(runtimeRoomData);
    }
}
