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

    public TurnOrderPanelController turnOrderPanelController;

    public List<UnitController> UnitControllerTurnOrder;
    public int CurrentTurnIndex;

    public struct InitiativeEntry
    {
        public UnitController unit;
        public float roll;
    }

    private List<InitiativeEntry> initiativeList = new();

    public static Action<UnitController> OnRefreshUI;
    public static Action<UnitController> OnActionPhaseCompleted;

    public static Action OnTurnEnded;
    public static Action OnBattleEnded;


    //TODO: move this to a UI manager
    public CombatMenuController CombatMenuController;

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    private void Start()
    {
        OnActionPhaseCompleted += ProcessEndOfTurnEffects;
        OnTurnEnded += MoveToNextTurn;
        ProgressionManager.OnRoomLoaded += GenerateTurnOrder;
    }

    void OnDestroy()
    {
        OnActionPhaseCompleted -= ProcessEndOfTurnEffects;
        OnTurnEnded -= MoveToNextTurn;
        ProgressionManager.OnRoomLoaded -= GenerateTurnOrder;
    }

    public void RemoveUnitFromTurnEntries(UnitController controller)
    {
        if (UnitControllerTurnOrder.Contains(controller))
        {
            turnOrderPanelController.RemoveTurnEntry(controller);
            UnitControllerTurnOrder.Remove(controller);
        }
    }

    private void GenerateTurnOrder()
    {
        initiativeList.Clear();

        foreach (var unit in BattleManager.instance.FriendlyUnits)
        {
            initiativeList.Add(new InitiativeEntry
            {
                unit = unit,
                roll = Mathf.Round(UnityEngine.Random.Range(unit.CombatStats.InitiativeMin, unit.CombatStats.InitiativeMax) * 10f) / 10f
            });
        }

        foreach (var unit in BattleManager.instance.EnemyUnits)
        {
            initiativeList.Add(new InitiativeEntry
            {
                unit = unit,
                roll = Mathf.Round(UnityEngine.Random.Range(unit.CombatStats.InitiativeMin, unit.CombatStats.InitiativeMax) * 10f) / 10f
            });
        }

        initiativeList = initiativeList.OrderByDescending(x => x.roll).ToList();
        UnitControllerTurnOrder = initiativeList.Select(x => x.unit).ToList();

        for (int i = 0; i < initiativeList.Count; i++)
        {
            turnOrderPanelController.CreateTurnEntry(initiativeList[i].unit, initiativeList[i].roll);
        }
        CurrentTurnIndex = -1;
        MoveToNextTurn();
    }

    private void MoveToNextTurn()
    {
        if (IsBattleOver())
        {
            OnBattleEnded?.Invoke();
            return;
        }

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

        CombatMenuController.CurrentUnitController = currentUnit;

        turnOrderPanelController.SetActiveTurnEntry(currentUnit);

        currentUnit.RegenerateResources();
        OnRefreshUI?.Invoke(currentUnit);
        currentUnit.ProcessStatusEffects();
        currentUnit.BeginActionPhase();
    }

    private void ProcessEndOfTurnEffects(UnitController controller)
    {
        controller.ProcessEndTurnEffects();
        OnTurnEnded?.Invoke();
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
