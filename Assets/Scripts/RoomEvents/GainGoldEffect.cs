using UnityEngine;

[CreateAssetMenu(fileName = "Events", menuName = "Events/EventEffect/GainGoldEffect")]
public class GainGoldEffect : EventEffect
{
    public int amount;
    public override void Execute()
    {
        Debug.Log("Gained " + amount + " gold");
        ProgressionManager.instance.LoadNextRoom();
    }
}
