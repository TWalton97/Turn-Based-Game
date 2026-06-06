using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CombatLogController : MonoBehaviour
{
    public static CombatLogController instance;

    public Color ExperienceColor;
    public int TotalExp;
    public bool LeveledUp = false;

    public Color HealingColor;
    public float TotalHealing;

    public Dictionary<ItemSO, int> items = new();

    public Transform CombatLogParent;
    public TextMeshProUGUI CombatLogEntryPrefab;

    private List<GameObject> spawnedCombatLogEntries = new();

    public float durationBetweenLogEntries = 0.5f;
    void Awake()
    {
        if (instance == null)
            instance = this;
    }

    public void AddExpToCombatLog(int amount)
    {
        TotalExp += amount;
    }

    public void AddLevelUp()
    {
        LeveledUp = true;
    }

    public void AddHealingToCombatLog(float amount)
    {
        TotalHealing += amount;
    }

    public void AddItemToCombatlog(ItemSO item, int amount)
    {
        if (items.ContainsKey(item))
        {
            items[item] += amount;
        }
        else
        {
            items.Add(item, amount);
        }
    }

    public IEnumerator DisplayCombatLogAndClear()
    {
        TextMeshProUGUI spawnedEntry;
        spawnedEntry = Instantiate(CombatLogEntryPrefab, CombatLogParent);
        spawnedEntry.text = $"+{TotalExp} Exp";
        spawnedEntry.color = ExperienceColor;
        spawnedCombatLogEntries.Add(spawnedEntry.gameObject);
        yield return new WaitForSeconds(durationBetweenLogEntries);

        if (LeveledUp)
        {
            spawnedEntry = Instantiate(CombatLogEntryPrefab, CombatLogParent);
            spawnedEntry.text = $"Lvl Up!";
            spawnedEntry.color = ExperienceColor;
            spawnedCombatLogEntries.Add(spawnedEntry.gameObject);
            LeveledUp = false;
            yield return new WaitForSeconds(durationBetweenLogEntries);
        }

        spawnedEntry = Instantiate(CombatLogEntryPrefab, CombatLogParent);
        spawnedEntry.text = $"+{TotalHealing} HP";
        spawnedEntry.color = HealingColor;
        spawnedCombatLogEntries.Add(spawnedEntry.gameObject);
        yield return new WaitForSeconds(durationBetweenLogEntries);

        foreach (ItemSO item in items.Keys)
        {
            spawnedEntry = Instantiate(CombatLogEntryPrefab, CombatLogParent);
            spawnedEntry.text = $"+{items[item]} {item.ItemName}";
            spawnedCombatLogEntries.Add(spawnedEntry.gameObject);
            yield return new WaitForSeconds(durationBetweenLogEntries);
        }

        yield return new WaitForSeconds(2f);
        foreach (GameObject obj in spawnedCombatLogEntries)
        {
            Destroy(obj);
        }
        Clear();
    }

    public void Clear()
    {
        TotalExp = 0;
        TotalHealing = 0;
        items.Clear();
    }
}


