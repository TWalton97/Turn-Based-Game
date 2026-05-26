using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Recipe")]
public class RecipeSO : ScriptableObject
{
    public List<InventoryEntry> CraftingIngredients;
    public ItemSO CraftingOutput;

    public bool IsRecipeUnlocked(List<ItemSO> collectedItems)
    {
        foreach (InventoryEntry entry in CraftingIngredients)
        {
            if (!collectedItems.Contains(entry.Item))
                return false;
        }
        return true;
    }
}
