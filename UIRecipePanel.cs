using System.Collections.Generic;
using System.Linq;
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
			elem.VAlign = 0.5f;
			elem.Left.Pixels = offset;
			Append(elem);
			offset += width + 10;
		};

		appendElement(new UIItemPanel(_recipe.createItem, 50), 50);

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
