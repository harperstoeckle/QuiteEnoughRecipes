using System.Collections.Generic;
using System.Linq;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Map;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

// Displays a recipe; similar to what you might see in the crafting window.
public class UIRecipePanel : UIAutoExtend
{
	/*
	 * Sometimes we want to show recipes that aren't real recipes (like shimmer), so we want to
	 * create a new `Recipe` object. However, Terraria will throw an exception if we try to
	 * construct a `Recipe` in the wrong context, so we instead have to use this stuff directly.
	 */
	public UIRecipePanel(Item createItem, List<Item>? requiredItems = null,
		List<int>? acceptedGroups = null, List<int>? requiredTiles = null,
		List<Condition>? conditions = null)
	{
		requiredItems ??= new();
		acceptedGroups ??= new();
		requiredTiles ??= new();
		conditions ??= new();

		Height.Pixels = 50;
		Width.Percent = 1;

		float offset = 0;

		var appendElement = (UIElement elem, float width) => {
			elem.Left.Pixels = offset;
			Append(elem);
			offset += width + 10;
		};

		appendElement(new UIItemPanel(createItem, 50), 50);

		var conditionStrings =
			requiredTiles.Select(CraftingStationName)
			.Concat(conditions.Select(c => c.Description.Value));
		var conditionText = string.Join(", ", conditionStrings);

		var constraintTextPanel = new UIText(conditionText, 0.6f);
		constraintTextPanel.Left.Pixels = offset;

		Append(constraintTextPanel);

		var requiredItemsContainer = new UIAutoExtendGrid();
		requiredItemsContainer.Width = new StyleDimension(-60, 1);
		requiredItemsContainer.Top.Pixels = 20;
		requiredItemsContainer.HAlign = 1;

		foreach (var item in requiredItems)
		{
			// See if there's a group in the recipe that accepts this item.
			var maybeGroup = acceptedGroups
				.Select(g => {
					RecipeGroup.recipeGroups.TryGetValue(g, out var rg);
					return rg;
				}).FirstOrDefault(rg => rg.ContainsItem(item.type), null);

			var elem = maybeGroup == null
					? new UIItemPanel(item, 30)
					: new UIRecipeGroupPanel(maybeGroup, item.stack, 30);

			requiredItemsContainer.Append(elem);
		}

		Append(requiredItemsContainer);
	}

	public UIRecipePanel(Recipe recipe) :
		this(recipe.createItem, recipe.requiredItem, recipe.acceptedGroups, recipe.requiredTile,
			recipe.Conditions)
	{
	}

	public UIRecipePanel(FakeRecipe recipe) :
	this(recipe.CreateItem, recipe.RequiredItems, recipe.AcceptedGroups, recipe.RequiredTiles,
		recipe.Conditions)
	{
	}

	private static string CraftingStationName(int tileID)
	{
		return tileID == -1
			? "?"
			: Lang.GetMapObjectName(MapHelper.TileToLookup(tileID, Recipe.GetRequiredTileStyle(tileID)));
	}
}

public record FakeRecipe(Item CreateItem, List<Item>? RequiredItems = null,
		List<int>? AcceptedGroups = null, List<int>? RequiredTiles = null,
		List<Condition>? Conditions = null);
