using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class UnitController : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public bool IsActiveTurn = false;

    public string UnitName;

    public int MaxHealth = 30;
    public int CurrentHealth;

    public int MaxMana = 5;
    public int CurrentMana = 1;

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

    private void Awake()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        enemyController = GetComponent<EnemyController>();
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


    public void UpdateHealth(int damage)
    {
        if (!IsAlive) return;

        CurrentHealth = Mathf.Clamp(CurrentHealth - damage, 0, MaxHealth);
        DamageNumberManager.instance.SpawnDamageNumberAtPosition(damage, transform.position);
        OnHealthValueChanged?.Invoke();
        if (CurrentHealth == 0)
            Die();
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
        UpdateMana(ability.ManaCost);
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
            controller.UpdateHealth(ability.DamageAmount);
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
}
