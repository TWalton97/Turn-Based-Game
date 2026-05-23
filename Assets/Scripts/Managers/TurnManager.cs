using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    //This class just handles figuring out the turn order
    //It also calls to specific units when their turn starts
    public static TurnManager instance;

    public List<UnitController> UnitControllerTurnOrder;
    public int CurrentTurnIndex;

    public struct InitiativeEntry
    {
        public UnitController unit;
        public int roll;
    }

    private List<InitiativeEntry> initiativeList = new();

    public static Action OnTurnEnded;

    private void Awake()
    {
        if (instance == null)
            instance = this;

        OnTurnEnded += MoveToNextTurn;
    }

    void OnDestroy()
    {
        OnTurnEnded -= MoveToNextTurn;
    }

    private void Start()
    {
        GenerateTurnOrder();
    }

    private void GenerateTurnOrder()
    {
        foreach (var unit in BattleManager.instance.FriendlyUnits)
        {
            initiativeList.Add(new InitiativeEntry
            {
                unit = unit,
                roll = UnityEngine.Random.Range(1, 21)
            });
        }

        foreach (var unit in BattleManager.instance.EnemyUnits)
        {
            initiativeList.Add(new InitiativeEntry
            {
                unit = unit,
                roll = UnityEngine.Random.Range(1, 21)
            });
        }

        initiativeList = initiativeList.OrderByDescending(x => x.roll).ToList();
        UnitControllerTurnOrder = initiativeList.Select(x => x.unit).ToList();
        MoveToNextTurn();
    }

    private void MoveToNextTurn()
    {
        if (IsBattleOver())
            return;

        int attempts = 0;

        do
        {
            CurrentTurnIndex =
                (CurrentTurnIndex + 1) % UnitControllerTurnOrder.Count;

            attempts++;

            if (attempts >= UnitControllerTurnOrder.Count)
            {
                Debug.Log("No alive units remaining.");
                return;
            }
        }
        while (!UnitControllerTurnOrder[CurrentTurnIndex].IsAlive);

        UnitController currentUnit =
            UnitControllerTurnOrder[CurrentTurnIndex];

        currentUnit.OnTurnStarted?.Invoke();
    }

    private bool IsBattleOver()
    {
        bool allFriendliesDead = BattleManager.instance.FriendlyUnits.All(u => !u.IsAlive);

        bool allEnemiesDead = BattleManager.instance.EnemyUnits.All(u => !u.IsAlive);

        if (allFriendliesDead)
        {
            Debug.Log("All friendlies dead");
            return true;
        }


        if (allEnemiesDead)
        {
            Debug.Log("All enemies dead");
            return true;
        }


        return false;
    }
}
