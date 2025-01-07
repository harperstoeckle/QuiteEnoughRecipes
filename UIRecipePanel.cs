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

		var appendItem = (Item item, float width) => {
			var panel = new UIItemPanel(item, width);
			panel.VAlign = 0.5f;
			panel.Left.Pixels = offset;
			Append(panel);
			offset += width + 10;
		};

		appendItem(_recipe.createItem, 50);

		foreach (var item in _recipe.requiredItem)
		{
			appendItem(item, 30);
		}
	}
}
