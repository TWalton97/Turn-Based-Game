using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    //This class basically just stores data relevant to each player
    //Data can get passed onto the spawn manager to spawn players into each room

    public static PlayerDataManager instance;

    public List<UnitController> PlayerUnitControllers;

    public void Awake()
    {
        if (instance == null)
            instance = this;
    }
}
