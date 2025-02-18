using System.Collections.Generic;
using Terraria.Localization;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

/*
 * Represents a tab in the recipe panel. This can be used for sources, uses, or both; the
 * distinction is made by the `QueryType` passed to `GetRecipeDisplays`.
 */
public interface IRecipeHandler
{
	// The name that will be shown when this tab is hovered.
	public LocalizedText HoverName { get; }

	// Item icon that will be displayed in the tab.
	public Item TabItem { get; }

	/*
	 * Given an ingredient, returns a sequence of UI elements showing recipes that either use or
	 * result in that ingredient.
	 */
	public IEnumerable<UIElement> GetRecipeDisplays(IIngredient ing, QueryType queryType);
}

public enum QueryType
{
	Sources,
	Uses
}
