using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageNumberManager : MonoBehaviour
{
    public static DamageNumberManager instance;

    public DamageNumber DamageNumberPrefab;

    void Awake()
    {
        if (instance == null)
            instance = this;
    }

    public void SpawnDamageNumberAtPosition(DamageResult result, Vector3 position)
    {
        DamageNumber damageNumber = Instantiate(DamageNumberPrefab);
        damageNumber.transform.position = position + Vector3.up;
        damageNumber.SetText(result.Damage, result.Crit);
    }

    public void SpawnDodgedTextAtPosition(Vector3 position)
    {
        DamageNumber damageNumber = Instantiate(DamageNumberPrefab);
        damageNumber.transform.position = position + Vector3.up;
        damageNumber.SetDodgeText();
    }
}
