using System.Collections.Generic;
using System.Linq;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

// Displays a recipe; similar to what you might see in the crafting window.
public class UIRecipePanel : UIElement
{
	private Recipe _recipe;

	public UIRecipePanel(Recipe recipe)
	{
		_recipe = recipe;
		Height.Pixels = 50;
		Width.Percent = 100;
	}

	public override void OnInitialize()
	{
		float offset = 0;

		var appendElement = (UIElement elem, float width) => {
			elem.Left.Pixels = offset;
			Append(elem);
			offset += width + 10;
		};

		appendElement(new UIItemPanel(_recipe.createItem, 50), 50);

		var conditionStrings =
			_recipe.requiredTile.Select(id => TileID.Search.TryGetName(id, out var s) ? s : "?")
			.Concat(_recipe.Conditions.Select(c => c.Description.Value));
		var conditionText = string.Join(", ", conditionStrings);

		var constraintTextPanel = new UIText(conditionText, 0.6f);
		constraintTextPanel.Left.Pixels = offset;
		constraintTextPanel.VAlign = 1;

		Append(constraintTextPanel);

		foreach (var item in _recipe.requiredItem)
		{
			// See if there's a group in the recipe that accepts this item.
			var maybeGroup = _recipe.acceptedGroups
				.Select(g => {
					RecipeGroup.recipeGroups.TryGetValue(g, out var rg);
					return rg;
				}).FirstOrDefault(rg => rg.ContainsItem(item.type), null);

			var elem = maybeGroup == null
					? new UIItemPanel(item, 30)
					: new UIRecipeGroupPanel(maybeGroup, item.stack, 30);

			appendElement(elem, 30);
		}
	}
}
