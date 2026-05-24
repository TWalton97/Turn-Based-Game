using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageNumberManager : MonoBehaviour
{
    public static DamageNumberManager instance;

    public DamageNumber DamageNumberPrefab;
    public Transform DamageNumberParent;

    void Awake()
    {
        if (instance == null)
            instance = this;
    }

    public void SpawnDamageNumberAtPosition(float value, Vector3 position)
    {
        DamageNumber damageNumber = Instantiate(DamageNumberPrefab);
        damageNumber.transform.position = position + Vector3.up;
        damageNumber.SetText(value);
    }
}
