using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitController : MonoBehaviour
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
    private WorldSpaceUnitUI worldSpaceUI;
    public GameObject characterModel;

    public Action OnTurnStarted;

    public EnemyController enemyController { get; private set; }

    private void Awake()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        worldSpaceUI = GetComponentInChildren<WorldSpaceUnitUI>();
        enemyController = GetComponent<EnemyController>();
        OnTurnStarted += StartTurn;
    }

    private void OnDestroy()
    {
        OnTurnStarted -= StartTurn;
    }

    private void Start()
    {
        CurrentHealth = MaxHealth;
        worldSpaceUI.UnitName.text = UnitName;
        worldSpaceUI.HealthBarText.text = CurrentHealth + "/" + MaxHealth;
        worldSpaceUI.HealthBarFill.fillAmount = (float)CurrentHealth / MaxHealth;
        worldSpaceUI.ManaBarText.text = CurrentMana + "/" + MaxMana;
        worldSpaceUI.ManaBarFill.fillAmount = (float)CurrentMana / MaxMana;
    }

    private void StartTurn()
    {
        IsActiveTurn = true;
        UpdateMana(-1);

        //This is where we can trigger status effects

        if (enemyController == null)
            return;

        enemyController.PickAction();
    }

    public void UpdateHealth(int damage)
    {
        if (!IsAlive) return;

        CurrentHealth = Mathf.Clamp(CurrentHealth - damage, 0, MaxHealth);
        worldSpaceUI.HealthBarFill.fillAmount = (float)CurrentHealth / (float)MaxHealth;
        worldSpaceUI.HealthBarText.text = CurrentHealth + "/" + MaxHealth;
        if (CurrentHealth == 0)
            Die();
    }

    public void UpdateMana(int amount)
    {
        if (!IsAlive) return;

        CurrentMana = Mathf.Clamp(CurrentMana - amount, 0, MaxMana);
        worldSpaceUI.ManaBarFill.fillAmount = (float)CurrentMana / (float)MaxMana;
        worldSpaceUI.ManaBarText.text = CurrentMana + "/" + MaxMana;
    }

    private void Die()
    {
        Debug.Log(gameObject.name + " has died");
        gameObject.SetActive(false);
        IsAlive = false;
    }

    public void EnableHighlight()
    {
        meshRenderer.material.EnableKeyword("_EMISSION");
    }

    public void DisableHighlight()
    {
        meshRenderer.material.DisableKeyword("_EMISSION");
    }
}
