using System.Collections.Generic;
using System.Linq;
using System;
using Terraria.GameContent.ItemDropRules;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

public static class RecipeHandlers
{
	// Normal recipes.
	public class Basic : IRecipeHandler
	{
		public LocalizedText HoverName { get; }
			= Language.GetText("Mods.QuiteEnoughRecipes.Tabs.Recipes");

		public Item TabItem { get; } = new(ItemID.WorkBench);

		public IEnumerable<UIElement> GetRecipeDisplays(IIngredient ing, QueryType queryType)
		{
			if (!(ing is ItemIngredient i)) { yield break; }

			foreach (var r in Main.recipe)
			{
				if (queryType == QueryType.Sources && r.createItem.type == i.Item.type
					|| queryType == QueryType.Uses && RecipeAcceptsItem(r, i.Item))
				{
					yield return new UIRecipePanel(r);
				}
			}
		}
	}

	// Recipes requiring a crafting station.
	public class CraftingStations : IRecipeHandler
	{
		public LocalizedText HoverName { get; }
			= Language.GetText("Mods.QuiteEnoughRecipes.Tabs.Tiles");

		public Item TabItem { get; } = new(ItemID.Furnace);

		public IEnumerable<UIElement> GetRecipeDisplays(IIngredient ing, QueryType queryType)
		{
			if (!(queryType == QueryType.Uses && ing is ItemIngredient i))
			{
				yield break;
			}

			foreach (var r in Main.recipe)
			{
				if (r.requiredTile.Contains(i.Item.createTile))
				{
					yield return new UIRecipePanel(r);
				}
			}
		}
	}

	public class ShimmerTransmutations : IRecipeHandler
	{
		public LocalizedText HoverName { get; }
			= Language.GetText("Mods.QuiteEnoughRecipes.Tabs.Shimmer");
		
		public Item TabItem { get; } = new(ItemID.BottomlessShimmerBucket);

		public IEnumerable<UIElement> GetRecipeDisplays(IIngredient ing, QueryType queryType)
		{
			if (!(ing is ItemIngredient i)) { yield break; }

			if (queryType == QueryType.Sources)
			{
				for (int id = 0; id < ItemID.Sets.ShimmerTransformToItem.Length; ++id)
				{
					if (ShimmerTransformResult(id) == i.Item.type)
					{
						yield return new UIRecipePanel(new(i.Item.type), [new(id)]);
					}
				}
			}
			else
			{
				int id = ShimmerTransformResult(i.Item.type);
				if (id == -1) { yield break; }
				yield return new UIRecipePanel(new(id), [new(i.Item.type)]);
			}
		}
	}

	public class NPCShops : IRecipeHandler
	{
		public LocalizedText HoverName { get; }
			= Language.GetText("Mods.QuiteEnoughRecipes.Tabs.Shops");

		public Item TabItem { get; } = new(ItemID.GoldCoin);

		public IEnumerable<UIElement> GetRecipeDisplays(IIngredient ing, QueryType queryType)
		{
			switch (ing, queryType)
			{
			case (ItemIngredient i, QueryType.Sources):
				foreach (var shop in NPCShopDatabase.AllShops)
				{
					if (shop.ActiveEntries.Any(e => e.Item.type == i.Item.type))
					{
						yield return new UINPCShopPanel(shop);
					}
				}

				break;

			case (ItemIngredient i, QueryType.Uses):
				foreach (var shop in NPCShopDatabase.AllShops)
				{
					if (shop.ActiveEntries.Any(e => MatchesCurrency(i.Item, e.Item)))
					{
						yield return new UINPCShopPanel(shop);
					}
				}

				break;

			case (NPCIngredient n, QueryType.Uses):
				foreach (var shop in NPCShopDatabase.AllShops)
				{
					if (shop.NpcType == n.ID)
					{
						yield return new UINPCShopPanel(shop);
					}
				}

				break;
			}
		}

		static bool MatchesCurrency(Item currency, Item shopEntry)
		{
			if (CustomCurrencyManager.TryGetCurrencySystem(shopEntry.shopSpecialCurrency, out var customCurrency))
			{
				return customCurrency.Accepts(currency);
			}
			return currency.IsACoin;
		}
	}

	// Result of opening loot items like treasure bags and goodie bags.
	public class ItemDrops : IRecipeHandler
	{
		public LocalizedText HoverName { get; }
			= Language.GetText("Mods.QuiteEnoughRecipes.Tabs.ItemDrops");

		public Item TabItem { get; } = new(ItemID.CultistBossBag);

		public IEnumerable<UIElement> GetRecipeDisplays(IIngredient ing, QueryType queryType)
		{
			if (!(ing is ItemIngredient i)) { yield break; }

			if (queryType == QueryType.Sources)
			{
				var item = new Item();
				for (int itemID = 0; itemID < ItemLoader.ItemCount; ++itemID)
				{
					item.SetDefaults(itemID);

					var droppedItems = GetItemDrops(item.type);
					if (droppedItems.Any(info => info.itemId == i.Item.type))
					{
						yield return new UIDropsPanel(
							new UIItemPanel(item.Clone(), 70), droppedItems);
					}
				}
			}
			else
			{
				var droppedItems = GetItemDrops(i.Item.type);
				if (droppedItems.Count > 0)
				{
					yield return new UIDropsPanel(new UIItemPanel(i.Item, 70), droppedItems);
				}
			}
		}
	}

	// Items dropped by NPCs when they are killed.
	public class NPCDrops : IRecipeHandler
	{
		public LocalizedText HoverName { get; }
			= Language.GetText("Mods.QuiteEnoughRecipes.Tabs.NPCDrops");

		public Item TabItem { get; } = new(ItemID.ZombieArm);

		public IEnumerable<UIElement> GetRecipeDisplays(IIngredient ing, QueryType queryType)
		{
			switch (ing, queryType)
			{
			case (ItemIngredient i, QueryType.Sources):
				for (int id = 0; id < NPCLoader.NPCCount; ++id)
				{
					/*
					 * TODO: Is there a better way to check whether a bestiary entry exists?
					 *
					 * For some reason, `FindEntryByNPCID` doesn't return null, but *does*
					 * return an entry with a null icon.
					 */
					if (Main.BestiaryDB.FindEntryByNPCID(id).Icon == null) { continue; }

					var droppedItems = GetNPCDrops(id);
					if (droppedItems.Any(info => info.itemId == i.Item.type))
					{
						yield return new UIDropsPanel(new UINPCPanel(id){
							Width = new StyleDimension(72, 0),
							Height = new StyleDimension(72, 0)
						}, droppedItems);
					}
				}

				break;

			case (NPCIngredient n, QueryType.Uses):
				if (Main.BestiaryDB.FindEntryByNPCID(n.ID).Icon == null) { yield break; }

				var drops = GetNPCDrops(n.ID);
				if (drops.Count > 0)
				{
					yield return new UIDropsPanel(new UINPCPanel(n.ID){
						Width = new StyleDimension(72, 0),
						Height = new StyleDimension(72, 0)
					}, drops);
				}

				break;
			}
		}
	}

	// If the item can be dropped from *any* NPC, this tab will show.
	public class GlobalDrops : IRecipeHandler
	{
		public LocalizedText HoverName { get; }
			= Language.GetText("Mods.QuiteEnoughRecipes.Tabs.GlobalDrops");

		public Item TabItem { get; } = new(ItemID.Heart);

		public IEnumerable<UIElement> GetRecipeDisplays(IIngredient ing, QueryType queryType)
		{
			if (!(queryType == QueryType.Sources && ing is ItemIngredient i))
			{
				yield break;
			}

			var drops = GetGlobalDrops();
			if (drops.Any(info => info.itemId == i.Item.type))
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

	// Get items that will be listed as drops from an NPC with ID `id`.
	private static List<DropRateInfo> GetNPCDrops(int id)
	{
		var bestiaryDrops = GetDropsFromRules(Main.ItemDropsDB.GetRulesForNPCID(id, false));
		var bannerDrop = GetBannerDrop(id);

		/*
		 * TODO: Prepending the banner is slow, so do this a different way.
		 *
		 * The banner needs to be prepended to make sure it's the first item in the `UIDropsPanel`.
		 * What should probably *eventually* happen is that `UIDropsPanel` should, on its own, sort
		 * banners before other items. However, it's actually kind of hard to tell if an item is a
		 * banner because there doesn't seem to be an `ItemToBanner` function.
		 */
		if (bannerDrop != null) { bestiaryDrops.Insert(0, bannerDrop.Value); }
		return bestiaryDrops;
	}

	private static List<DropRateInfo> GetGlobalDrops()
	{
		return GetDropsFromRules(new GlobalLoot(Main.ItemDropsDB).Get());
	}

	private static List<DropRateInfo> GetDropsFromRules(IEnumerable<IItemDropRule> rules)
	{
		/*
		 * TODO: It would be much more efficient to fill an existing list rather than making a new
		 * one each time.
		 */
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
	private static DropRateInfo? GetBannerDrop(int id)
	{
		var banner = Item.NPCtoBanner(id);
		if (banner == 0) return null;
		var bannerItem = Item.BannerToItem(banner);
		var killRequirement = ItemID.Sets.KillsToBanner[bannerItem];
		return new DropRateInfo(bannerItem, 1, 1, 1, [new BannerDropCondition(killRequirement)]);
	}
}
