using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UnitController : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public bool IsActiveTurn = false;

    public string UnitName;

    public UnitStats UnitStats;
    public CombatStats CombatStats;

    public ClassStatPresetSO UnitData;
    public int MaxHealth = 30;
    public int CurrentHealth;

    public int MaxMana = 5;
    public int CurrentMana = 0;

    public bool IsAlive = true;

    public Team UnitTeam;

    public List<BaseAbility> Abilities;

    private MeshRenderer meshRenderer;

    public Action OnManaValueChanged;
    public Action OnHealthValueChanged;
    public Action OnDie;

    private List<UnitController> unitControllers = new();

    public EnemyController enemyController { get; private set; }
    private Vector3 startPos;

    public Dictionary<Attribute, Action<float>> statSetters;

    private void Awake()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        enemyController = GetComponent<EnemyController>();

        statSetters = new Dictionary<Attribute, Action<float>>
        {
            { Attribute.STR, v => UnitStats.Strength += (int)v },
            { Attribute.DEX, v => UnitStats.Dexterity += (int)v },
            { Attribute.CON, v => UnitStats.Constitution += (int)v },
            { Attribute.INT, v => UnitStats.Intelligence += (int)v },
            { Attribute.FTH, v => UnitStats.Faith += (int)v },
            { Attribute.CHA, v => UnitStats.Charisma += (int)v },
            { Attribute.LCK, v => UnitStats.Luck += (int)v },
        };

        ApplyClassPresetStats();
        RecalculateCombatStats();
    }

    private void ApplyClassPresetStats()
    {
        MaxHealth = UnitData.MaxHealth;
        CurrentHealth = MaxHealth;

        MaxMana = UnitData.MaxMana;

        UnitStats.Strength = UnitData.Strength;
        UnitStats.Dexterity = UnitData.Dexterity;
        UnitStats.Constitution = UnitData.Constitution;
        UnitStats.Intelligence = UnitData.Intelligence;
        UnitStats.Faith = UnitData.Faith;
        UnitStats.Charisma = UnitData.Charisma;
        UnitStats.Luck = UnitData.Luck;
    }

    public void RegenerateResources()
    {
        //TODO: determine how many resources to restore
        UpdateMana(-1);
    }

    public void ProcessStatusEffects()
    {
        //TODO: status effects will go here
    }

    public void BeginActionPhase()
    {
        IsActiveTurn = true;
        //If there is an enemy controller, let it decide
        //Otherwise we want to display the UI
        if (enemyController == null)
            return;

        enemyController.PickAction();
    }

    public void ProcessEndTurnEffects()
    {
        //TODO: end of turn effects will go here
    }

    public void UpdateHealth(int amount)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0, MaxHealth);
    }

    public void UpdateHealth(DamageResult damageResult)
    {
        if (!IsAlive) return;

        if (damageResult.Dodged)
        {
            DamageNumberManager.instance.SpawnDodgedTextAtPosition(transform.position);
        }
        else
        {
            DamageNumberManager.instance.SpawnDamageNumberAtPosition(damageResult, transform.position);
            CurrentHealth = (int)Mathf.Clamp(CurrentHealth - damageResult.Damage, 0, MaxHealth);
            OnHealthValueChanged?.Invoke();
            if (CurrentHealth <= 0)
                Die();
        }
    }

    public void UpdateMana(int amount)
    {
        if (!IsAlive) return;

        CurrentMana = Mathf.Clamp(CurrentMana - amount, 0, MaxMana);
        OnManaValueChanged?.Invoke();
    }

    private void Die()
    {
        Debug.Log(gameObject.name + " has died");
        OnDie?.Invoke();
        gameObject.SetActive(false);
        TurnManager.instance.RemoveUnitFromTurnEntries(this);
        IsAlive = false;
    }

    public void TryUseAbility(BaseAbility ability, UnitController selectedTarget)
    {
        IsActiveTurn = false;
        ability.ConsumeCost(this);
        //UpdateMana(ability.ManaCost);
        unitControllers = ReturnTargetControllers(ability, selectedTarget);
        StartCoroutine(ExecuteAbility(ability, selectedTarget));
    }

    private IEnumerator ExecuteAbility(BaseAbility ability, UnitController selectedTarget)
    {
        yield return new WaitForSeconds(0.5f);

        yield return MoveToTargetIfNeeded(ability, selectedTarget);

        //Play animation goes here
        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < ability.NumberOfHits; i++)
        {
            ApplyEffect(ability, selectedTarget);
            yield return new WaitForSeconds(ability.DurationBetweenHits);
        }

        yield return new WaitForSeconds(0.5f);

        yield return MoveToOriginalPositionIfNeeded(ability, selectedTarget);

        yield return new WaitForSeconds(0.5f);

        TurnManager.OnActionPhaseCompleted?.Invoke(this);
    }

    private IEnumerator MoveToTargetIfNeeded(BaseAbility ability, UnitController selectedTarget)
    {
        if (!ability.MovesToTarget)
            yield break;

        startPos = transform.position;
        Vector3 endPos = selectedTarget.transform.position + selectedTarget.transform.forward * 2f;
        yield return MoveTo(endPos, 0.4f);
    }

    private IEnumerator MoveToOriginalPositionIfNeeded(BaseAbility ability, UnitController selectedTarget)
    {
        if (!ability.MovesToTarget)
            yield break;

        Vector3 endPos = startPos;
        yield return MoveTo(endPos, 0.4f);
    }

    private IEnumerator MoveTo(Vector3 position, float duration)
    {
        Vector3 startPos = transform.position;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, position, elapsedTime / duration);
            yield return null;
        }
        yield break;
    }

    public void ApplyEffect(BaseAbility ability, UnitController selectedTarget)
    {
        foreach (UnitController controller in unitControllers)
        {
            DamageResult damageResult = CombatResolver.CalculateDamage(this, ability, selectedTarget);
            controller.UpdateHealth(damageResult);
        }
    }

    private List<UnitController> ReturnTargetControllers(BaseAbility ability, UnitController target)
    {
        unitControllers.Clear();

        if (ability.TargetType == TargetType.SingleUnit)
        {
            unitControllers.Add(target);
        }
        else
        {
            switch (ability.TeamTargeting)
            {
                case Team.Enemy:
                    foreach (UnitController controller in BattleManager.instance.EnemyUnits)
                    {
                        unitControllers.Add(controller);
                    }
                    break;

                case Team.Ally:
                    foreach (UnitController controller in BattleManager.instance.FriendlyUnits)
                    {
                        unitControllers.Add(controller);
                    }
                    break;
            }
        }
        return unitControllers;
    }

    public void EnableHighlight()
    {
        meshRenderer.material.EnableKeyword("_EMISSION");
    }

    public void DisableHighlight()
    {
        meshRenderer.material.DisableKeyword("_EMISSION");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        UIManager.instance.AssignContextMenu(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        EnableHighlight();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        DisableHighlight();
    }

    public void RecalculateCombatStats()
    {
        CombatStats.InitiativeMin = 1 + (UnitStats.Dexterity * 0.2f);
        CombatStats.InitiativeMax = 6 + (UnitStats.Dexterity * 0.3f);

        CombatStats.CritChance = 10.0f + (0.5f * UnitStats.Luck) + (0.2f * UnitStats.Dexterity);
        CombatStats.CritDamage = 50.0f + UnitStats.Luck + (0.5f * UnitStats.Dexterity);

        CombatStats.BlockChance = 10.0f + (0.5f * UnitStats.Constitution) + (0.2f * UnitStats.Strength);
        CombatStats.BlockDamageReduction = 50.0f + (0.2f * UnitStats.Constitution);

        CombatStats.DodgeChance = 10.0f + (0.5f * UnitStats.Luck) + (0.2f * UnitStats.Dexterity);

        CombatStats.Aggro = 100.0f + UnitStats.Constitution - UnitStats.Charisma;

        CombatStats.EnergyGain = 0.5f * UnitStats.Intelligence;

        CombatStats.LuckyDrop = 0.5f * UnitStats.Luck;

        CombatStats.OutgoingHealing = 100.0f + 0.5f * UnitStats.Faith;
        CombatStats.IncomingHealing = 100.0f + 0.5f * UnitStats.Faith;
    }
}

[Serializable]
public class UnitStats
{
    public int Strength;
    public int Dexterity;
    public int Constitution;
    public int Intelligence;
    public int Faith;
    public int Charisma;
    public int Luck;
}

[Serializable]
public class CombatStats
{
    public float InitiativeMin;
    public float InitiativeMax;
    public float CritChance;
    public float CritDamage;
    public float BlockChance;
    public float BlockDamageReduction;
    public float DodgeChance;
    public float Aggro;
    public float Lifesteal;
    public float EnergyGain;
    public float LuckyDrop;
    public float IncomingHealing;
    public float OutgoingHealing;
}
