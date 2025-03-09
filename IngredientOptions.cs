using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria;

using ItemGroup = Terraria.ID.ContentSamples.CreativeHelper.ItemGroup;

namespace QuiteEnoughRecipes;

public class IngredientOptionAttribute : Attribute
{
	public string Group;
	public int IconID;

	public IngredientOptionAttribute(string group, int iconID = 0)
	{
		Group = group;
		IconID = iconID;
	}
};

/*
 * Various predicates on items used for filtering. For the most part, I try to stick to the vanilla
 * creative item groups for categorization when possible. This may change in the future if it turns
 * out they're not accurate enough.
 */
static class IngredientOptions
{
	// Mostly used by `IngredientRegistry`.
	public record OptionButtonSpec<T>(T Value, int IconID, LocalizedText Name) {}

	public static readonly string KeyParent = "Mods.QuiteEnoughRecipes.OptionGroups";

	// Get list of option buttons for options of type `T`.
	public static IEnumerable<OptionButtonSpec<T>> GetOptionButtons<T>(string groupName)
		where T : Delegate
	{
		var heading = Language.Exists($"{KeyParent}.{groupName}.Name")
			? Language.GetText($"{KeyParent}.{groupName}.Name")
			: null;
		var group = new UIOptionGroup<T>(heading);

		return typeof(IngredientOptions).GetMethods(BindingFlags.Public | BindingFlags.Static)
			.SelectMany(m => GetOptionsFromMethod<T>(m, groupName));
	}

	// Check if `i` is in the creative item group `g`.
	public static bool IsInGroup(ItemIngredient i, params ItemGroup[] groups)
	{
		if (!ContentSamples.ItemCreativeSortingId.TryGetValue(i.Item.type, out var result))
		{
			return false;
		}

		return groups.Any(g => result.Group == g);
	}

	/*
	 * Make an option group for mods that are present in a master list of ingredients.
	 *
	 * TODO: Fit this into the reflection-based system.
	 */
	public static UIOptionGroup<Predicate<T>> MakeModFilterGroup<T>(IEnumerable<T> ingredients)
		where T : IIngredient
	{
		var keyParent = "Mods.QuiteEnoughRecipes.OptionGroups.Mods";
		var group = new UIOptionGroup<Predicate<T>>(Language.GetText($"{keyParent}.Name"));

		var mods = ingredients
			.Select(i => i.Mod)
			.Where(m => m != null)
			.Select(m => m!)
			.Distinct()
			.OrderBy(m => m.DisplayNameClean);

		foreach (var mod in mods)
		{
			var name = Language.GetText($"{keyParent}.ModName").WithFormatArgs(mod.DisplayNameClean);
			var icon = mod.ModSourceBestiaryInfoElement.GetFilterImage();
			var button = new UIOptionToggleButton<Predicate<T>>(i => i.Mod == mod, icon){
				HoverText = name
			};

			group.AddOption(button);
		}

		return group;
	}

	public static bool IsWeaponInDamageClass(ItemIngredient i, DamageClass dc) =>
		i.Item.CountsAsClass(dc) && IsWeapon(i);
	public static bool IsWeapon(ItemIngredient i) => !IsTool(i) && i.Item.damage > 0
		&& i.Item.ammo == AmmoID.None;

	#region Item Filters
	[IngredientOption("ItemFilters.Misc", ItemID.StoneBlock)]
	public static bool IsTile(ItemIngredient i) => IsInGroup(i, ItemGroup.CraftingObjects,
		ItemGroup.Torches, ItemGroup.Wood, ItemGroup.Crates, ItemGroup.PlacableObjects,
		ItemGroup.Blocks, ItemGroup.Rope, ItemGroup.Walls);

	[IngredientOption("ItemFilters.Misc", ItemID.Furnace)]
	public static bool IsCraftingStation(ItemIngredient i) => IsInGroup(i, ItemGroup.CraftingObjects);

	[IngredientOption("ItemFilters.Misc", ItemID.SuspiciousLookingEye)]
	public static bool IsBossSummon(ItemIngredient i) => IsInGroup(i, ItemGroup.BossItem,
		ItemGroup.BossSpawners);

	[IngredientOption("ItemFilters.Misc", ItemID.GoodieBag)]
	public static bool IsLootItem(ItemIngredient i) => IsInGroup(i, ItemGroup.Crates, ItemGroup.BossBags,
		ItemGroup.GoodieBags);

	[IngredientOption("ItemFilters.Misc", ItemID.InfernoPotion)]
	public static bool IsPotion(ItemIngredient i) => IsInGroup(i, ItemGroup.LifePotions,
		ItemGroup.ManaPotions, ItemGroup.BuffPotion, ItemGroup.Flask);

	[IngredientOption("ItemFilters.Misc", ItemID.Apple)]
	public static bool IsFood(ItemIngredient i) => IsInGroup(i, ItemGroup.Food);

	[IngredientOption("ItemFilters.Misc", ItemID.WoodFishingPole)]
	public static bool IsFishing(ItemIngredient i) => IsInGroup(i, ItemGroup.FishingRods,
		ItemGroup.FishingQuestFish, ItemGroup.Fish, ItemGroup.FishingBait);

	[IngredientOption("ItemFilters.Misc", ItemID.RedDye)]
	public static bool IsDye(ItemIngredient i) => IsInGroup(i, ItemGroup.DyeMaterial, ItemGroup.Dye,
		ItemGroup.HairDye);

	[IngredientOption("ItemFilters.Misc", ItemID.AnkletoftheWind)]
	public static bool IsAccessory(ItemIngredient i) => IsInGroup(i, ItemGroup.Accessories);

	[IngredientOption("ItemFilters.Misc", ItemID.ExoticEasternChewToy)]
	public static bool IsPet(ItemIngredient i) => IsInGroup(i, ItemGroup.VanityPet);

	[IngredientOption("ItemFilters.Misc", ItemID.SlimySaddle)]
	public static bool IsMount(ItemIngredient i) => IsInGroup(i, ItemGroup.Mount, ItemGroup.Minecart);

	[IngredientOption("ItemFilters.Misc", ItemID.CreativeWings)]
	public static bool IsWings(ItemIngredient i) => i.Item.wingSlot >= 0;

	[IngredientOption("ItemFilters.Misc", ItemID.GrapplingHook)]
	public static bool IsHook(ItemIngredient i) => IsInGroup(i, ItemGroup.Hook);

	[IngredientOption("ItemFilters.Misc", ItemID.CopperPickaxe)]
	public static bool IsTool(ItemIngredient i) => IsInGroup(i, ItemGroup.Pickaxe, ItemGroup.Axe,
		ItemGroup.Hammer);

	[IngredientOption("ItemFilters.Misc", ItemID.CopperChainmail)]
	public static bool IsArmor(ItemIngredient i) => IsInGroup(i, ItemGroup.Headgear, ItemGroup.Torso,
		ItemGroup.Pants) && !IsVanity(i);

	[IngredientOption("ItemFilters.Misc", ItemID.RedHat)]
	public static bool IsVanity(ItemIngredient i) => i.Item.vanity;

	[IngredientOption("ItemFilters.Weapons", ItemID.CopperShortsword)]
	public static bool IsMeleeWeapon(ItemIngredient i) => IsInGroup(i, ItemGroup.MeleeWeapon);

	[IngredientOption("ItemFilters.Weapons", ItemID.WoodenBow)]
	public static bool IsRangedWeapon(ItemIngredient i) => IsInGroup(i, ItemGroup.RangedWeapon);

	[IngredientOption("ItemFilters.Weapons", ItemID.WandofSparking)]
	public static bool IsMagicWeapon(ItemIngredient i) => IsInGroup(i, ItemGroup.MagicWeapon);

	[IngredientOption("ItemFilters.Weapons", ItemID.BabyBirdStaff)]
	public static bool IsSummonWeapon(ItemIngredient i) => IsInGroup(i, ItemGroup.SummonWeapon);

	[IngredientOption("ItemFilters.Weapons", ItemID.FlareGun)]
	public static bool IsClasslessWeapon(ItemIngredient i) => IsWeaponInDamageClass(i, DamageClass.Default);

	[IngredientOption("ItemFilters.Weapons")]
	public static IEnumerable<OptionButtonSpec<Predicate<ItemIngredient>>> CustomWeaponFilters(
		string keyParent)
	{
		// We only add the thrower class filter if there are mods that add throwing weapons.
		if (FindIconItemForDamageClass(DamageClass.Throwing) is Item iconItem)
		{
			Predicate<ItemIngredient> pred = i => i.Item.DamageType == DamageClass.Throwing
				&& IsWeapon(i);
			var name = Language.GetText($"{keyParent}.IsThrowingWeapon");

			/*
			 * Unlike the other modded damage classes, we only want to show throwing weapons that
			 * are *exactly* in the throwing class, since modded classes that just *derive* from
			 * throwing (like rogue) will also be shown below with the other modded classes.
			 */
			yield return new(pred, iconItem.type, name);
		}

		/*
		 * Modded damage classes, grouped by name. An item will be considered to fit into a group if
		 * it counts as any of the damage classes in that group. Note that if two mods add damage
		 * classes with the same name, they will be grouped together even if they're completely
		 * unrelated.
		 */
		var moddedDamageClassSets =
			Enumerable.Range(0, DamageClassLoader.DamageClassCount)
			.Select(i => DamageClassLoader.GetDamageClass(i))
			.GroupBy(c => BaseDamageClassName(c.DisplayName.Value))
			.Select(g => g.ToList())
			.Where(g => g.All(d => !(d is VanillaDamageClass)))
			.OrderBy(g => g[0].Mod?.Name ?? "");

		foreach (var dcs in moddedDamageClassSets)
		{
			var icon = dcs.Select(d => FindIconItemForDamageClass(d))
				.FirstOrDefault(n => n != null);

			// This damage class has no items, so we can't really make a filter for it.
			if (icon == null) { continue; }

			Predicate<ItemIngredient> pred = i => dcs.Any(dc => IsWeaponInDamageClass(i, dc));

			// Adjust the name so instead of "rogue damage Weapons", we get "Rogue Weapons".
			var name = Language.GetText($"{keyParent}.IsOtherWeapon")
				.WithFormatArgs(BaseDamageClassName(dcs[0].DisplayName.Value));

			yield return new(pred, icon.type, name);
		}
	}
	#endregion

	#region Item Sorts
	[IngredientOption("ItemSorts", ItemID.TreeStatue)]
	public static int ByCreative(ItemIngredient x, ItemIngredient y)
	{
		var xg = ContentSamples.CreativeHelper.GetItemGroup(x.Item, out int xo);
		var yg = ContentSamples.CreativeHelper.GetItemGroup(y.Item, out int yo);
		return (xg, xo, x.Item.type).CompareTo((yg, yo, y.Item.type));
	}

	[IngredientOption("ItemSorts", ItemID.AlphabetStatue1)]
	public static int ByID(ItemIngredient x, ItemIngredient y) =>
		x.Item.type.CompareTo(y.Item.type);

	[IngredientOption("ItemSorts", ItemID.AlphabetStatueA)]
	public static int ByName(ItemIngredient x, ItemIngredient y) => x.Name.CompareTo(y.Name);

	[IngredientOption("ItemSorts", ItemID.StarStatue)]
	public static int ByRarity(ItemIngredient x, ItemIngredient y) =>
		x.Item.rare.CompareTo(y.Item.rare);

	[IngredientOption("ItemSorts", ItemID.ChestStatue)]
	public static int ByValue(ItemIngredient x, ItemIngredient y) =>
		x.Item.value.CompareTo(y.Item.value);
	#endregion

	#region NPC Filters
	[IngredientOption("NPCFilters", ItemID.SlimeCrown)]
	public static bool IsBoss(NPCIngredient n) =>
		TryGetBestiaryInfoElement<BossBestiaryInfoElement>(n.ID) != null;

	[IngredientOption("NPCFilters", ItemID.GoldCoin)]
	public static bool IsTownNPC(NPCIngredient n) => TryGetNPC(n.ID)?.isLikeATownNPC ?? false;
	#endregion

	#region NPC Sorts
	[IngredientOption("NPCSorts", ItemID.TreeStatue)]
	public static int ByBestiary(NPCIngredient x, NPCIngredient y)
	{
		int xid = ContentSamples.NpcBestiarySortingId[x.ID];
		int yid = ContentSamples.NpcBestiarySortingId[y.ID];
		return xid.CompareTo(yid);
	}

	[IngredientOption("NPCSorts", ItemID.AlphabetStatue1)]
	public static int ByID(NPCIngredient x, NPCIngredient y) => x.ID.CompareTo(y.ID);

	[IngredientOption("NPCSorts", ItemID.AlphabetStatueA)]
	public static int ByName(NPCIngredient x, NPCIngredient y) => x.Name.CompareTo(y.Name);

	[IngredientOption("NPCSorts", ItemID.StarStatue)]
	public static int ByRarity(NPCIngredient x, NPCIngredient y)
	{
		int xRare = TryGetNPC(x.ID)?.rarity ?? 0;
		int yRare = TryGetNPC(y.ID)?.rarity ?? 0;
		return xRare.CompareTo(yRare);
	}
	#endregion

	/*
	 * Tries to find a low-rarity item to use as an icon for a damage class filter. When applying
	 * the filter, any weapon that counts for this damage class will be shown, but for the purposes
	 * of the icon, we only want something whose damage class is *exactly* `d`, in the hope that it
	 * will be more representative of the class.
	 */
	private static Item? FindIconItemForDamageClass(DamageClass d)
	{
		return Enumerable.Range(0, ItemLoader.ItemCount)
			.Select(i => new Item(i))
			.Where(i => i.DamageType == d && IsWeapon(new(i)))
			.MinBy(i => i.rare);
	}

	/*
	 * Tries to turn something like "rogue damage" into "Rogue", i.e., a single capitalized word.
	 *
	 * TODO: Try to figure out how to make this work with all languages. I currently assume that the
	 * display name will end with the word "damage", but this clearly doesn't work for other
	 * languages. One possibility is to just cut the last word off, regardless of what it is. This
	 * doesn't work for languages where the "damage" equivalent is at the start of the name, though.
	 * Alternatively, maybe I could just determine the common suffix and prefix text from the
	 * vanilla display names for the current localization and strip it.
	 */
	private static string BaseDamageClassName(string name)
	{
		var ti = new CultureInfo("en-US", false).TextInfo;
		return ti.ToTitleCase(
			Regex.Replace(name, @"^\s*(.*?)\s*damage\s*$", @"$1", RegexOptions.IgnoreCase));
	}

	private static T? TryGetBestiaryInfoElement<T>(int npcID)
	{
		return Main.BestiaryDB.FindEntryByNPCID(npcID).Info.OfType<T>().FirstOrDefault();
	}

	private static NPC? TryGetNPC(int npcID)
	{
		return ContentSamples.NpcsByNetId.TryGetValue(npcID, out var npc) ? npc : null;
	}

	/*
	 * Get option(s) represented by the method `m`. `T` must be a delegate type. `m` is handled as
	 * follows:
	 * - If `m` does not have an `IngredientOption` attribute, or if the group name specified in
	 *   the attribute is not `groupName`, then no buttons are returned.
	 * - If `m` is directly convertible to the delegate type `T` then it is added as an option
	 *   button directly based on the specification given in the `IngredientOption` attribute.
	 * - If `m` is convertible to a delegate of type `(string) ->
	 *   IEnumerable<OptionButtonSpec<T>>`, then it will be called with the group's localization
	 *   key as an argument, and the 
	 */
	private static IEnumerable<OptionButtonSpec<T>> GetOptionsFromMethod<T>(MethodInfo m,
		string groupName) where T : Delegate
	{
		var attr = m.GetCustomAttribute<IngredientOptionAttribute>();
		if (attr == null || attr.Group != groupName) { return []; }

		var pred = TryCreateDelegate<T>(m);
		if (pred != null)
		{
			var text = Language.GetText($"{KeyParent}.{attr.Group}.{m.Name}");
			return [new(pred, attr.IconID, text)];
		}

		var listGen = TryCreateDelegate<Func<string, IEnumerable<OptionButtonSpec<T>>>>(m);
		if (listGen != null)
		{
			return listGen($"{KeyParent}.{groupName}");
		}

		return [];
	}

	private static T? TryCreateDelegate<T>(MethodInfo m) where T : Delegate
	{
		var d = Delegate.CreateDelegate(typeof(T), m, false);
		return d == null ? null : (d as T);
	}
}
