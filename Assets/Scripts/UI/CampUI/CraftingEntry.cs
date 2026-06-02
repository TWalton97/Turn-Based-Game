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
            playerDataController.TryRemoveItemsForRecipe(Recipe);
            playerDataController.ServerAddItemToInventory(Recipe.CraftingOutput);
        }
    }

    public void InspectItem()
    {
        //Populate the details panel with this item's details
        CampManager.instance.PopulateDetailsPanel(Recipe.CraftingOutput.ItemName, Recipe.CraftingOutput.ItemInformation, Recipe.CraftingOutput.Description);
    }

    public string BuildRecipeString(List<InventoryEntry> recipeItems)
    {
        StringBuilder sb = new StringBuilder();

        foreach (var entry in recipeItems)
        {
            int ownedQuantity = CampManager.instance.TrackedUnitController.GetComponent<PlayerDataController>().InventoryItems
            .Where(x => x.Item == entry.Item)
            .Sum(x => x.Quantity);

            bool hasEnough = ownedQuantity >= entry.Quantity;

            if (hasEnough)
            {
                sb.AppendLine($"{entry.Item.ItemName} {entry.Quantity}");
            }
            else
            {
                sb.AppendLine(
                    $"<color=red>{entry.Item.ItemName} {entry.Quantity}</color>"
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
