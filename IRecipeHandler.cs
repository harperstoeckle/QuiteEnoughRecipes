using System.Collections.Generic;
using Terraria.Localization;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

/*
 * Represents a tab in the recipe panel. This can be used either for sources or usages of an item;
 * the distinction is made by whether the handler is added via `AddSourceHandler` or
 * `AddUsageHandler`.
 */
public interface IRecipeHandler
{
	// The name that will be shown when this tab is hovered.
	public LocalizedText HoverName { get; }

	// Item icon that will be displayed in the tab.
	public Item TabItem { get; }

	// Given an item, get a sequence of UI elements showing the recipes involving that item.
	public IEnumerable<UIElement> GetRecipeDisplays(Item i);
}
