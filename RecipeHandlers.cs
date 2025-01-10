using System.Collections.Generic;
using System.Linq;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

public static class RecipeHandlers
{
	// Source handler for normal crafting.
	public class BasicSourceHandler : IRecipeHandler
	{
		public LocalizedText HoverName { get; }
			= Language.GetText("Mods.QuiteEnoughRecipes.Tabs.Recipes");

		public Item TabItem { get; } = new(ItemID.WorkBench);

		public IEnumerable<UIElement> GetRecipeDisplays(Item i)
		{
			foreach (var r in Main.recipe)
			{
				if (r.createItem.type == i.type)
				{
					yield return new UIRecipePanel(r);
				}
			}
		}
	}

	// Usage handler for normal crafting.
	public class BasicUsageHandler : IRecipeHandler
	{
		public LocalizedText HoverName { get; }
			= Language.GetText("Mods.QuiteEnoughRecipes.Tabs.Recipes");

		public Item TabItem { get; } = new(ItemID.WorkBench);

		public IEnumerable<UIElement> GetRecipeDisplays(Item i)
		{
			foreach (var r in Main.recipe)
			{
				if (RecipeAcceptsItem(r, i))
				{
					yield return new UIRecipePanel(r);
				}
			}
		}
	}

	/*
 	 * Shows recipes that require a tile to be crafted. The tile is selected from whatever tile the item
 	 * will place.
 	 */
	public class TileUsageHandler : IRecipeHandler
	{
		public LocalizedText HoverName { get; }
			= Language.GetText("Mods.QuiteEnoughRecipes.Tabs.Tiles");

		public Item TabItem { get; } = new(ItemID.Furnace);

		public IEnumerable<UIElement> GetRecipeDisplays(Item i)
		{
			foreach (var r in Main.recipe)
			{
				if (r.requiredTile.Contains(i.createTile))
				{
					yield return new UIRecipePanel(r);
				}
			}
		}
	}

	// Show items that can be shimmered into the target item.
	public class ShimmerSourceHandler : IRecipeHandler
	{
		public LocalizedText HoverName { get; }
			= Language.GetText("Mods.QuiteEnoughRecipes.Tabs.Shimmer");
		
		public Item TabItem { get; } = new(ItemID.BottomlessShimmerBucket);

		public IEnumerable<UIElement> GetRecipeDisplays(Item i)
		{
			for (int id = 0; id < ItemID.Sets.ShimmerTransformToItem.Length; ++id)
			{
				if (ShimmerTransformResult(id) == i.type)
				{
					yield return new UIRecipePanel(i, new List<Item>{new(id)});
				}
			}
		}
	}

	// Show the result of shimmering the target item.
	public class ShimmerUsageHandler : IRecipeHandler
	{
		public LocalizedText HoverName { get; }
			= Language.GetText("Mods.QuiteEnoughRecipes.Tabs.Shimmer");
		
		public Item TabItem { get; } = new(ItemID.BottomlessShimmerBucket);

		public IEnumerable<UIElement> GetRecipeDisplays(Item i)
		{
			int id = ShimmerTransformResult(i.type);
			if (id == -1) { yield break; }
			yield return new UIRecipePanel(new(id), new List<Item>{i});
		}
	}

	private static bool RecipeAcceptsItem(Recipe r, Item i)
	{
		return r.requiredItem.Any(x => x.type == i.type)
			|| r.acceptedGroups.Any(
				g => RecipeGroup.recipeGroups.TryGetValue(g, out var rg) && rg.ContainsItem(i.type)
			);
	}

	/*
	 * Returns the item ID of the shimmer result of transforming an item with ID `inputItem`. If no
	 * such output exists, returns -1.
	 */
	public static int ShimmerTransformResult(int inputItem)
	{
		int id = ItemID.Sets.ShimmerCountsAsItem[inputItem];
		if (id == -1) { id = inputItem; }
		return ItemID.Sets.ShimmerTransformToItem[id];
	}
}
