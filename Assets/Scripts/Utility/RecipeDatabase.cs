using System.Collections.Generic;

public static class RecipeDatabase
{
    public static Dictionary<string, RecipeSO> Recipes = new();

    public static RecipeSO GetRecipeByName(string recipeName)
    {
        return Recipes[recipeName];
    }

    public static void RegisterRecipe(RecipeSO recipeSO)
    {
        if (Recipes.ContainsValue(recipeSO))
            return;

        Recipes.Add(recipeSO.CraftingOutput.ItemName, recipeSO);
    }
}
