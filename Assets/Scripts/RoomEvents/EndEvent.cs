using UnityEngine;

[CreateAssetMenu(fileName = "Events", menuName = "Events/EventEffect/EndEventEffect")]
public class EndEvent : EventEffect
{
    public override void Execute()
    {
        ProgressionManager.instance.LoadNextRoom();
    }
}
