using System.Collections.Generic;
using System.Linq;
using Terraria.GameContent.ItemDropRules;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria;
using System;

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
					yield return new UIRecipePanel(new(i.type), new List<Item>{new(id)});
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
			yield return new UIRecipePanel(new(id), new List<Item>{new(i.type)});
		}
	}

	// Shows NPC shops that sell the given item.
	public class NPCShopSourceHandler : IRecipeHandler
	{
		public LocalizedText HoverName { get; }
			= Language.GetText("Mods.QuiteEnoughRecipes.Tabs.Shops");

		public Item TabItem { get; } = new(ItemID.GoldCoin);

		public IEnumerable<UIElement> GetRecipeDisplays(Item i)
		{
			foreach (var shop in NPCShopDatabase.AllShops)
			{
				if (shop.ActiveEntries.Any(e => e.Item.type == i.type))
				{
					yield return new UINPCShopPanel(shop);
				}
			}
		}
	}

	// Shows items that can drop the given item when used. I.e., treasure bags.
	public class ItemDropSourceHandler : IRecipeHandler
	{
		public LocalizedText HoverName { get; }
			= Language.GetText("Mods.QuiteEnoughRecipes.Tabs.ItemDrops");

		public Item TabItem { get; } = new(ItemID.CultistBossBag);

		public IEnumerable<UIElement> GetRecipeDisplays(Item i)
		{
			var item = new Item();

			foreach (int itemID in Enumerable.Range(0, ItemLoader.ItemCount))
			{
				item.SetDefaults(itemID);

				var droppedItems = GetItemDrops(item.type);
				if (droppedItems.Any(info => info.itemId == i.type))
				{
					yield return new UIDropsPanel(new UIItemPanel(item.Clone(), 70), droppedItems);
				}
			}
		}
	}

	// Shows items dropped when using the given item.
	public class ItemDropUsageHandler : IRecipeHandler
	{
		public LocalizedText HoverName { get; }
			= Language.GetText("Mods.QuiteEnoughRecipes.Tabs.ItemDrops");

		public Item TabItem { get; } = new(ItemID.CultistBossBag);

		public IEnumerable<UIElement> GetRecipeDisplays(Item i)
		{
			var droppedItems = GetItemDrops(i.type);
			if (droppedItems.Count > 0)
			{
				yield return new UIDropsPanel(new UIItemPanel(i, 70), droppedItems);
			}
		}
	}

	// Shows what NPCs drop the given item.
	public class NPCDropSourceHandler : IRecipeHandler
	{
		public LocalizedText HoverName { get; }
			= Language.GetText("Mods.QuiteEnoughRecipes.Tabs.NPCDrops");

		public Item TabItem { get; } = new(ItemID.ZombieArm);

		public IEnumerable<UIElement> GetRecipeDisplays(Item i)
		{
			foreach (int id in Enumerable.Range(0, NPCLoader.NPCCount))
			{
				/*
				 * TODO: Is there a better way to check whether a bestiary entry exists?
				 *
				 * For some reason, `FindEntryByNPCID` doesn't return null, but *does* return an
				 * entry with a null icon.
				 */
				if (Main.BestiaryDB.FindEntryByNPCID(id).Icon == null) { continue; }

				var droppedItems = GetNPCDrops(id);
				var bannerItem = GetBannerDrop(id);
				droppedItems.Add(bannerItem);
				if (droppedItems.Any(info => info.itemId == i.type))
				{
					yield return new UIDropsPanel(new UINPCPanel(id){
						Width = new StyleDimension(82, 0),
						Height = new StyleDimension(82, 0)
					}, droppedItems);
				}
			}
		}
	}

	// If the item can be dropped from any NPC, this tab will show.
	public class GlobalLootSourceHandler : IRecipeHandler
	{
		public LocalizedText HoverName { get; }
			= Language.GetText("Mods.QuiteEnoughRecipes.Tabs.GlobalDrops");

		public Item TabItem { get; } = new(ItemID.Heart);

		public IEnumerable<UIElement> GetRecipeDisplays(Item i)
		{
			var drops = GetGlobalDrops();

			if (drops.Any(info => info.itemId == i.type))
			{
				/*
				 * Cheaty way to do this. The "left element" that would usually be a portrait is
				 * empty, so all that's visible is the grid.
				 */
				yield return new UIDropsPanel(new UIElement(), drops);
			}
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
	private static int ShimmerTransformResult(int inputItem)
	{
		int id = ItemID.Sets.ShimmerCountsAsItem[inputItem];
		if (id == -1) { id = inputItem; }
		return ItemID.Sets.ShimmerTransformToItem[id];
	}

	// Get items that can be dropped when using the item with ID `itemID`.
	private static List<DropRateInfo> GetItemDrops(int itemID)
	{
		return GetDropsFromRules(Main.ItemDropsDB.GetRulesForItemID(itemID));
	}

	/*
	 * Get items that can be dropped by the NPC with ID `id`. Only items that would be displayed in
	 * the bestiary are included.
	 */
	private static List<DropRateInfo> GetNPCDrops(int id)
	{
		// We want to ignore common drops.
		return GetDropsFromRules(Main.ItemDropsDB.GetRulesForNPCID(id, false));
	}

	private static List<DropRateInfo> GetGlobalDrops()
	{
		return GetDropsFromRules(new GlobalLoot(Main.ItemDropsDB).Get());
	}

	private static List<DropRateInfo> GetDropsFromRules(IEnumerable<IItemDropRule> rules)
	{
		var results = new List<DropRateInfo>();
		var feed = new DropRateInfoChainFeed(1);

		foreach (var rule in rules)
		{
			rule.ReportDroprates(results, feed);
		}

		results.RemoveAll(info => info.conditions?.Any(c => !c.CanShowItemDropInUI()) ?? false);
		return results;
	}

	private record BannerDropCondition(int Kills) : IItemDropRuleCondition
	{
		public bool CanDrop(DropAttemptInfo info) => true;

		public bool CanShowItemDropInUI() => true;

		public string GetConditionDescription() => Language.GetText("Mods.QuiteEnoughRecipes.Conditions.BannerDrop").WithFormatArgs(Kills).Value;
	}
	private static DropRateInfo GetBannerDrop(int id)
	{
		var banner = Item.NPCtoBanner(id);
		if (banner == 0) return default;
		var bannerItem = Item.BannerToItem(banner);
		var killRequirement = ItemID.Sets.KillsToBanner[bannerItem];
		return new DropRateInfo(bannerItem, 1, 1, 1, [new BannerDropCondition(killRequirement)]);
	}
}
