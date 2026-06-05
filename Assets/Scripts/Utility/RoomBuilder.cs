using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RoomBuilder
{
    public static RuntimeRoomData GenerateRoomData(int currentRoomIndex, List<UnitController> allAreaEnemies)
    {
        RuntimeRoomData runtimeRoomData = new();

        int roomBudget = 7 + (currentRoomIndex * 3);
        int enemyCount = 2;
        if (roomBudget >= 18)
        {
            enemyCount = Random.value < 0.8f ? 2 : 3;
        }
        int enemyBudget = roomBudget / enemyCount;
        List<CombatRoomEnemyEntry> validEnemies = ReturnValidEnemies(enemyBudget, allAreaEnemies);
        for (int i = 0; i < enemyCount; i++)
        {
            int rand = Random.Range(0, validEnemies.Count);
            runtimeRoomData.Enemies.Add(validEnemies[rand]);
        }
        return runtimeRoomData;
    }

    private static List<CombatRoomEnemyEntry> ReturnValidEnemies(int strengthPerEnemy, List<UnitController> allAreaEnemies)
    {
        List<CombatRoomEnemyEntry> validRoomEnemyEntries = new();

        foreach (UnitController controller in allAreaEnemies)
        {
            EnemyController enemyController = controller.GetComponent<EnemyController>();
            if (enemyController == null)
                continue;

            if (enemyController.baseStrength > strengthPerEnemy)
                continue;

            int maxLevel = GetMaxLevelForTarget(enemyController, strengthPerEnemy);
            CombatRoomEnemyEntry entry = new();
            entry.Unit = controller;
            entry.Level = maxLevel;
            validRoomEnemyEntries.Add(entry);
        }

        return validRoomEnemyEntries;
    }

    private static int GetMaxLevelForTarget(EnemyController enemy, int targetStrength)
    {
        int raw = targetStrength - enemy.baseStrength + 1;
        return Mathf.Max(1, raw);
    }
}
