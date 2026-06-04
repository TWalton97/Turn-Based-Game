using System.Collections;
using System.Collections.Generic;
using UnityEditor.Presets;
using UnityEngine;

public class RoomPresetDatabase : MonoBehaviour
{
    public List<CombatRoom> combatRoomPresets;
    private Dictionary<int, CombatRoom> lookup;
    public static RoomPresetDatabase instance;

    void Awake()
    {
        if (instance == null)
            instance = this;

        Build();
    }

    void Build()
    {
        lookup = new Dictionary<int, CombatRoom>();

        foreach (var preset in combatRoomPresets)
        {
            if (preset == null)
                continue;

            if (lookup.ContainsKey(preset.presetId))
            {
                Debug.LogError($"Duplicate preset ID: {preset.presetId}");
                continue;
            }

            lookup[preset.presetId] = preset;
        }
    }

    public bool TryGetRoomById(int presetId, out CombatRoom preset)
    {
        return lookup.TryGetValue(presetId, out preset);
    }
}
