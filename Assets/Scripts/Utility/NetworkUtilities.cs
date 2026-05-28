using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public static class NetworkUtilities
{
    public static UnitController GetUnitControllerById(ulong unitId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(unitId, out NetworkObject obj))
        {
            return obj.GetComponent<UnitController>();
        }

        Debug.LogWarning($"No UnitController exists with id {unitId}");
        return null;
    }
}
