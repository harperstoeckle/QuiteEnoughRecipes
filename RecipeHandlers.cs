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
using System.Reflection;
using System.Runtime.CompilerServices;
using Terraria.ModLoader.Core;

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

	// Shows the tiles that a given item drops from
	public class TileDropsSourceHandler : IRecipeHandler
	{
		public LocalizedText HoverName { get; }
			= Language.GetText("Mods.QuiteEnoughRecipes.Tabs.TileDrops");

		public Item TabItem { get; } = new(ItemID.StoneBlock);

		public IEnumerable<UIElement> GetRecipeDisplays(Item i)
		{
			var tiles = GetTilesThatDropItem(i.type);
			foreach (var (id, style) in tiles)
			{
				/*
				 * A style of -1 means that all styles drop the item (unless an explicit style entry exists)
				 * But since -1 is an invalid style it has to be skipped
				 * Note: style is ignored for unframed tiles
				 */
				if (style == -1 && Main.tileFrameImportant[id]) { continue; }

				var safeStyle = Math.Max(0, style);
				yield return new UIDropsPanel(new UITilePanel(id, safeStyle), [new(i.type, 1, 1, 1)]);
			}
		}
	}

	// Shows the tiles placed by a given item 
	public class CreateTileUsageHandler : IRecipeHandler
	{
		public LocalizedText HoverName { get; }
			= Language.GetText("Mods.QuiteEnoughRecipes.Tabs.CreatedTiles");

		public Item TabItem { get; } = new(ItemID.StoneBlock);

		public IEnumerable<UIElement> GetRecipeDisplays(Item i)
		{
			if (i.createTile != -1)
			{
				yield return new UIDropsPanel(new UITilePanel(i.createTile, i.placeStyle), [new(i.type, 1, 1, 1)]);
			}
		}
	}

	// Shows the walls that a given item drops from
	public class WallDropsSourceHandler : IRecipeHandler
	{
		public LocalizedText HoverName { get; }
			= Language.GetText("Mods.QuiteEnoughRecipes.Tabs.WallDrops");

		public Item TabItem { get; } = new(ItemID.StoneWall);

		public IEnumerable<UIElement> GetRecipeDisplays(Item i)
		{
			var walls = GetWallsThatDropItem(i.type);
			foreach (var wall in walls)
			{
				yield return new UIDropsPanel(new UIWallPanel(wall), [new(i.type, 1, 1, 1)]);
			}
		}
	}

	// Shows the walls placed by a given item 
	public class CreateWallUsageHandler : IRecipeHandler
	{
		public LocalizedText HoverName { get; }
			= Language.GetText("Mods.QuiteEnoughRecipes.Tabs.CreatedWalls");

		public Item TabItem { get; } = new(ItemID.StoneWall);

		public IEnumerable<UIElement> GetRecipeDisplays(Item i)
		{
			if (i.createWall != -1)
			{
				yield return new UIDropsPanel(new UIWallPanel(i.createWall), [new(i.type, 1, 1, 1)]);
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

	internal static FieldInfo TileLoader_tileTypeAndTileStyleToItemType = typeof(TileLoader).GetField("tileTypeAndTileStyleToItemType", BindingFlags.Static | BindingFlags.NonPublic);
	internal static Dictionary<(int TileId, int Style), int> TileTypeAndTileStyleToItemType => TileLoader_tileTypeAndTileStyleToItemType.GetValue(null) as Dictionary<(int, int), int>;

	private static Dictionary<int, List<(int TileId, int Style)>>? _itemTypeToTileTypeAndTileStyle = null;
	internal static Dictionary<int, List<(int TileId, int Style)>> ItemTypeToTileTypeAndTileStyle =>
		_itemTypeToTileTypeAndTileStyle ??= TileTypeAndTileStyleToItemType.GroupBy(entry => entry.Value)
			.ToDictionary(entry => entry.Key, entry => entry.Select(entry => entry.Key).ToList());

	private static Dictionary<int, List<(int Style, int Item)>>? _tileTypeToTileStyleAndItemType = null;
	internal static Dictionary<int, List<(int Style, int Item)>> TileTypeToTileStyleAndItemType =>
		_tileTypeToTileStyleAndItemType ??= TileTypeAndTileStyleToItemType.GroupBy(entry => entry.Key.TileId)
			.ToDictionary(entry => entry.Key, entry => entry.Select(entry => (entry.Key.Style, entry.Value)).ToList());

	private static List<(int TileId, int Style)> GetTilesThatDropItem(int itemId)
	{
		if (ItemTypeToTileTypeAndTileStyle.TryGetValue(itemId, out var tile))
		{
			return tile;
		}
		return [];
	}

	internal static FieldInfo WallLoader_wallTypeToItemType = typeof(WallLoader).GetField("wallTypeToItemType", BindingFlags.Static | BindingFlags.NonPublic);
	internal static Dictionary<int, int> WallTypeToItemType => WallLoader_wallTypeToItemType.GetValue(null) as Dictionary<int, int>;

	[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "KillWall_GetItemDrops")]
	extern static int WorldGen_KillWall_GetItemDrops(WorldGen self, Tile tile);

	private static Dictionary<int, int> VanillaWallDrops()
	{
		Dictionary<int, int> lookup = [];
		var tempTile = new Tile();
		var wallIdCache = tempTile.WallType;
		for (int i = 0; i < WallID.Count; i++)
		{
			tempTile.Get<WallTypeData>().Type = (ushort)i;
			// Vanilla only checks WallType when determining the drop
			var drop = WorldGen_KillWall_GetItemDrops(null, tempTile);
			if (drop != 0)
			{
				lookup[i] = drop;
			}
		}
		// Tiles are heavily manged by TML so resetting it to it's initial state is important
		tempTile.Get<WallTypeData>().Type = wallIdCache;
		return lookup;
	}

	private static Dictionary<int, List<int>>? _itemTypeToWallType = null;
	internal static Dictionary<int, List<int>> ItemTypeToWallType => _itemTypeToWallType ??= WallTypeToItemType
			.Where(entry => WallLoader.GetWall(entry.Value) != null && !LoaderUtils.HasOverride(WallLoader.GetWall(entry.Value), m => m.Drop))
			.Concat(VanillaWallDrops())
			.GroupBy(entry => entry.Value)
			.ToDictionary(group => group.Key, group => group.Select(entry => entry.Key).ToList());

	private static List<int> GetWallsThatDropItem(int itemId)
	{
		if (ItemTypeToWallType.TryGetValue(itemId, out var walls))
		{
			return walls;
		}
		return [];
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
