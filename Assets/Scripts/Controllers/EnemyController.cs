using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    //Extra script added to UnitControllers that we want to be controlled by AI
    //All this has to do is choose a random action and random target when it becomes this unit's turn

    public UnitController unitController;
    public UnitController target;
    public List<UnitController> unitControllers = new();

    private void Awake()
    {
        unitController = GetComponent<UnitController>();
    }

    //Find valid abilities based on specific factors
    //Do I have enough mana + is there a valid target?
    //Pick an option from the list of abilities, weighted towards higher mana, etc...

    public void PickAction()
    {
        List<BaseAbility> validAbilities = ReturnListOfValidAbilities();

        if (validAbilities.Count == 0)
        {
            Debug.Log("No valid abilities found, ending turn");
            unitController.IsActiveTurn = false;
            BattleManager.OnTurnEnded?.Invoke();
            return;
        }

        BaseAbility chosenAbility = ChooseAbility(validAbilities);
        List<UnitController> validTargets = GetValidTargets(chosenAbility);
        target = validTargets[Random.Range(0, validTargets.Count)];
        unitControllers = ReturnTargetControllers(chosenAbility);
        unitController.UpdateMana(chosenAbility.ManaCost);
        Debug.Log("Using ability " + chosenAbility.AbilityName);
        StartCoroutine(PlayAttackAnimation(chosenAbility));
    }

    private List<BaseAbility> ReturnListOfValidAbilities()
    {
        List<BaseAbility> validAbilities = new();

        foreach (BaseAbility ability in unitController.Abilities)
        {
            if (ability.ManaCost > unitController.CurrentMana)
                continue;

            if (GetValidTargets(ability).Count == 0)
                continue;

            validAbilities.Add(ability);
        }

        return validAbilities;
    }

    private List<UnitController> GetValidTargets(BaseAbility ability)
    {
        List<UnitController> targets =
        ability.TeamTargeting == Team.Enemy
        ? BattleManager.instance.EnemyUnits
        : BattleManager.instance.FriendlyUnits;

        targets = targets
        .Where(t => t.IsAlive)
        .ToList();

        if (ability.DamageType == DamageType.Heal)
        {
            targets = targets
                .Where(t => t.CurrentHealth < t.MaxHealth)
                .ToList();
        }

        return targets;
    }

    private BaseAbility ChooseAbility(List<BaseAbility> validAbilities)
    {
        int totalWeight = 0;

        foreach (BaseAbility ability in validAbilities)
        {
            totalWeight += ability.ManaCost;
        }

        int roll = Random.Range(0, totalWeight);

        foreach (BaseAbility ability in validAbilities)
        {
            roll -= ability.ManaCost;

            if (roll < 0)
                return ability;
        }

        return validAbilities[0];
    }
    private IEnumerator PlayAttackAnimation(BaseAbility ability)
    {
        Vector3 startPos = unitController.characterModel.transform.position;
        Vector3 targetPos = target.characterModel.transform.position + (target.characterModel.transform.forward * 2f);
        float elapsedTime = 0f;

        while (elapsedTime < 0.2f)
        {
            elapsedTime += Time.deltaTime;
            unitController.characterModel.transform.position = Vector3.Lerp(startPos, targetPos, elapsedTime / 0.2f);
            yield return null;
        }

        yield return new WaitForSeconds(0.8f);
        foreach (UnitController controller in unitControllers)
            controller.UpdateHealth(ability.DamageAmount);

        elapsedTime = 0f;
        while (elapsedTime < 0.2f)
        {
            elapsedTime += Time.deltaTime;
            unitController.characterModel.transform.position = Vector3.Lerp(targetPos, startPos, elapsedTime / 0.2f);
            yield return null;
        }

        unitController.characterModel.transform.position = startPos;
        unitController.IsActiveTurn = false;
        BattleManager.OnTurnEnded?.Invoke();
        yield return null;
    }

    private List<UnitController> ReturnTargetControllers(BaseAbility ability)
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
}
