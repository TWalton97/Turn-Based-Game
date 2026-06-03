using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Recipe")]
public class RecipeSO : ScriptableObject
{
    public List<RecipeIngredient> CraftingIngredients;
    public ItemSO CraftingOutput;

    public bool IsRecipeUnlocked(List<ItemSO> collectedItems)
    {
        foreach (RecipeIngredient ingredient in CraftingIngredients)
        {
            if (!collectedItems.Contains(ingredient.item))
                return false;
        }
        return true;
    }

    public void OnEnable()
    {
        RecipeDatabase.RegisterRecipe(this);
    }
}

[System.Serializable]
public class RecipeIngredient
{
    public ItemSO item;
    public int quantity;
}
