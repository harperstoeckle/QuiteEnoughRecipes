using System.Collections.Generic;
using Terraria.Localization;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

/*
 * Represents a tab in the recipe panel. This can be used either for sources or usages of an
 * ingredient; the distinction is made by whether the handler is added via `AddSourceHandler` or
 * `AddUsageHandler`.
 */
public interface IRecipeHandler
{
	// The name that will be shown when this tab is hovered.
	public LocalizedText HoverName { get; }

	// Item icon that will be displayed in the tab.
	public Item TabItem { get; }

	// Given an ingredient, get a sequence of UI elements showing the recipes involving it.
	public IEnumerable<UIElement> GetRecipeDisplays(IIngredient ing);
}
