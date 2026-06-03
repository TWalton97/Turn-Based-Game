using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CraftingEntry : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public RecipeSO Recipe;
    public TextMeshProUGUI RecipeNameText;

    public Button CraftButton;
    public Button InspectButton;

    private PlayerDataController playerDataController;

    public Color CanCraftColor;
    public Color CannotCraftColor;

    public void AssignRecipe(RecipeSO recipe)
    {
        Recipe = recipe;
        RecipeNameText.text = recipe.CraftingOutput.ItemName;

        playerDataController = CampManager.instance.TrackedUnitController.GetComponent<PlayerDataController>();
        CheckIfCraftingAvailable();
        playerDataController.OnInventoryUpdated += CheckIfCraftingAvailable;
    }

    void OnDestroy()
    {
        playerDataController.OnInventoryUpdated -= CheckIfCraftingAvailable;
    }

    public void CheckIfRecipeUnlocked()
    {
        if (!Recipe.IsRecipeUnlocked(CampManager.instance.CollectedItems))
        {
            gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(true);
        }
    }

    public void CheckIfCraftingAvailable()
    {
        TextMeshProUGUI craftButtonText = CraftButton.GetComponentInChildren<TextMeshProUGUI>();
        if (playerDataController.HasItemsForRecipe(Recipe))
        {
            craftButtonText.color = CanCraftColor;
        }
        else
        {
            craftButtonText.color = CannotCraftColor;
        }
    }

    public void TryCraftItem()
    {
        if (playerDataController.HasItemsForRecipe(Recipe))
        {
            playerDataController.TryCraftItemServerRpc(Recipe.CraftingOutput.ItemName);
        }
    }

    public void InspectItem()
    {
        //Populate the details panel with this item's details
        CampManager.instance.PopulateDetailsPanel(Recipe.CraftingOutput.ItemName, Recipe.CraftingOutput.ItemInformation, Recipe.CraftingOutput.Description);
    }

    public string BuildRecipeString(List<RecipeIngredient> recipeItems)
    {
        StringBuilder sb = new StringBuilder();

        foreach (var ingredient in recipeItems)
        {
            ItemSO item = ItemDatabase.GetItemByName(ingredient.item.ItemName.ToString());
            int ownedQuantity = playerDataController.FindInventoryEntryByItem(item).quantity;

            bool hasEnough = ownedQuantity >= ingredient.quantity;

            if (hasEnough)
            {
                sb.AppendLine($"{ingredient.item.ItemName} {ingredient.quantity}");
            }
            else
            {
                sb.AppendLine(
                    $"<color=red>{ingredient.item.ItemName} {ingredient.quantity}</color>"
                );
            }
        }

        return sb.ToString().TrimEnd();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        TooltipManager.instance.EnableTooltipAtPosition(new TooltipData(
            BuildRecipeString(Recipe.CraftingIngredients),
            "",
            "",
            ""),
            eventData.position + new Vector2(150, 0),
            this
            );
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.DisableTooltipIfSource?.Invoke(this);
    }
}
