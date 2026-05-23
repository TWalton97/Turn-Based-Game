using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public static BattleManager instance;

    //Unit and slot references
    public List<BattleSlot> BattleSlots;
    public List<UnitController> AllUnits;
    public List<UnitController> FriendlyUnits;
    public List<UnitController> EnemyUnits;

    //Current battle state
    public bool IsBattleActive;

    public List<UnitController> UnitControllerTurnOrder;
    public int CurrentTurnIndex;

    public GameObject PlayerUI;

    //Turn order handling
    public struct InitiativeEntry
    {
        public UnitController unit;
        public int roll;
    }

    private List<InitiativeEntry> initiativeList = new List<InitiativeEntry>();

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

    //TODO: update this to roll initiative based on some value
    private void Start()
    {
        foreach (var unit in FriendlyUnits)
        {
            initiativeList.Add(new InitiativeEntry
            {
                unit = unit,
                roll = UnityEngine.Random.Range(1, 21)
            });
        }

        foreach (var unit in EnemyUnits)
        {
            initiativeList.Add(new InitiativeEntry
            {
                unit = unit,
                roll = UnityEngine.Random.Range(1, 21)
            });
        }

        initiativeList = initiativeList.OrderByDescending(x => x.roll).ToList();
        UnitControllerTurnOrder = initiativeList.Select(x => x.unit).ToList();
        //MoveToNextTurn();
    }

    public void RegisterUnit(UnitController controller)
    {
        AllUnits.Add(controller);

        if (controller.UnitTeam == Team.Ally)
            FriendlyUnits.Add(controller);

        if (controller.UnitTeam == Team.Enemy)
            EnemyUnits.Add(controller);
    }

    public void UnregisterUnit(UnitController controller)
    {
        if (AllUnits.Contains(controller))
            AllUnits.Remove(controller);

        if (FriendlyUnits.Contains(controller))
            FriendlyUnits.Remove(controller);

        if (EnemyUnits.Contains(controller))
            EnemyUnits.Remove(controller);
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

        PlayerUI.SetActive(currentUnit.enemyController == null);
    }

    private bool IsBattleOver()
    {
        bool allFriendliesDead = FriendlyUnits.All(u => !u.IsAlive);

        bool allEnemiesDead = EnemyUnits.All(u => !u.IsAlive);

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

    public BattleSlot ReturnEmptyBattleSlotOfType(Team team)
    {
        for (int i = 0; i < BattleSlots.Count; i++)
        {
            if (BattleSlots[i].Team == team && BattleSlots[i].UnitController == null)
                return BattleSlots[i];
        }
        Debug.Log("No battle slot available");
        return null;
    }

}
