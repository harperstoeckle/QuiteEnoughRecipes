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
	public static readonly string KeyParent = "Mods.QuiteEnoughRecipes.OptionGroups";

	// Get options in the given item group that are of type `T`.
	public static UIOptionGroup<T> GetOptionGroup<T>(string groupName) where T : Delegate
	{
		var heading = Language.Exists($"{KeyParent}.{groupName}.Name")
			? Language.GetText($"{KeyParent}.{groupName}.Name")
			: null;
		var group = new UIOptionGroup<T>(heading);

		var optionButtons = typeof(IngredientOptions)
			.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.SelectMany(m => GetOptionsFromMethod<T>(m, groupName));
		foreach (var button in optionButtons) { group.AddOption(button); }

		return group;
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

	/*
	 * Get a option group of filters for ingredients of type `T`, but with the filters made
	 * generic so that they can be applied to any ingredient.
	 */
	public static IOptionElement<Predicate<IIngredient>> GetGenericFilterGroup<T>(
		string groupName) where T : IIngredient
	{
		return GetOptionGroup<Predicate<T>>(groupName).Map(MakeGenericPredicate<T, IIngredient>);
	}

	// Same as `GetGenericFilterGroup`, but for comparisons.
	public static IOptionElement<Comparison<IIngredient>> GetGenericSortGroup<T>(
		string groupName) where T : IIngredient
	{
		return GetOptionGroup<Comparison<T>>(groupName)
			.Map(MakeGenericComparison<T, IIngredient>);
	}

	public static bool IsWeaponInDamageClass(ItemIngredient i, DamageClass dc) =>
		i.Item.CountsAsClass(dc) && IsWeapon(i);
	public static bool IsWeapon(ItemIngredient i) => !IsTool(i) && i.Item.damage > 0
		&& i.Item.ammo == AmmoID.None;

	#region Item Filters
	[IngredientOption("ItemFilters", ItemID.StoneBlock)]
	public static bool IsTile(ItemIngredient i) => IsInGroup(i, ItemGroup.CraftingObjects,
		ItemGroup.Torches, ItemGroup.Wood, ItemGroup.Crates, ItemGroup.PlacableObjects,
		ItemGroup.Blocks, ItemGroup.Rope, ItemGroup.Walls);

	[IngredientOption("ItemFilters", ItemID.Furnace)]
	public static bool IsCraftingStation(ItemIngredient i) => IsInGroup(i, ItemGroup.CraftingObjects);

	[IngredientOption("ItemFilters", ItemID.SuspiciousLookingEye)]
	public static bool IsBossSummon(ItemIngredient i) => IsInGroup(i, ItemGroup.BossItem,
		ItemGroup.BossSpawners);

	[IngredientOption("ItemFilters", ItemID.GoodieBag)]
	public static bool IsLootItem(ItemIngredient i) => IsInGroup(i, ItemGroup.Crates, ItemGroup.BossBags,
		ItemGroup.GoodieBags);

	[IngredientOption("ItemFilters", ItemID.InfernoPotion)]
	public static bool IsPotion(ItemIngredient i) => IsInGroup(i, ItemGroup.LifePotions,
		ItemGroup.ManaPotions, ItemGroup.BuffPotion, ItemGroup.Flask);

	[IngredientOption("ItemFilters", ItemID.Apple)]
	public static bool IsFood(ItemIngredient i) => IsInGroup(i, ItemGroup.Food);

	[IngredientOption("ItemFilters", ItemID.WoodFishingPole)]
	public static bool IsFishing(ItemIngredient i) => IsInGroup(i, ItemGroup.FishingRods,
		ItemGroup.FishingQuestFish, ItemGroup.Fish, ItemGroup.FishingBait);

	[IngredientOption("ItemFilters", ItemID.RedDye)]
	public static bool IsDye(ItemIngredient i) => IsInGroup(i, ItemGroup.DyeMaterial, ItemGroup.Dye,
		ItemGroup.HairDye);

	[IngredientOption("ItemFilters", ItemID.AnkletoftheWind)]
	public static bool IsAccessory(ItemIngredient i) => IsInGroup(i, ItemGroup.Accessories);

	[IngredientOption("ItemFilters", ItemID.ExoticEasternChewToy)]
	public static bool IsPet(ItemIngredient i) => IsInGroup(i, ItemGroup.VanityPet);

	[IngredientOption("ItemFilters", ItemID.SlimySaddle)]
	public static bool IsMount(ItemIngredient i) => IsInGroup(i, ItemGroup.Mount, ItemGroup.Minecart);

	[IngredientOption("ItemFilters", ItemID.CreativeWings)]
	public static bool IsWings(ItemIngredient i) => i.Item.wingSlot >= 0;

	[IngredientOption("ItemFilters", ItemID.GrapplingHook)]
	public static bool IsHook(ItemIngredient i) => IsInGroup(i, ItemGroup.Hook);

	[IngredientOption("ItemFilters", ItemID.CopperPickaxe)]
	public static bool IsTool(ItemIngredient i) => IsInGroup(i, ItemGroup.Pickaxe, ItemGroup.Axe,
		ItemGroup.Hammer);

	[IngredientOption("ItemFilters", ItemID.CopperChainmail)]
	public static bool IsArmor(ItemIngredient i) => IsInGroup(i, ItemGroup.Headgear, ItemGroup.Torso,
		ItemGroup.Pants) && !IsVanity(i);

	[IngredientOption("ItemFilters", ItemID.RedHat)]
	public static bool IsVanity(ItemIngredient i) => i.Item.vanity;

	[IngredientOption("WeaponFilters", ItemID.CopperShortsword)]
	public static bool IsMeleeWeapon(ItemIngredient i) => IsInGroup(i, ItemGroup.MeleeWeapon);

	[IngredientOption("WeaponFilters", ItemID.WoodenBow)]
	public static bool IsRangedWeapon(ItemIngredient i) => IsInGroup(i, ItemGroup.RangedWeapon);

	[IngredientOption("WeaponFilters", ItemID.WandofSparking)]
	public static bool IsMagicWeapon(ItemIngredient i) => IsInGroup(i, ItemGroup.MagicWeapon);

	[IngredientOption("WeaponFilters", ItemID.BabyBirdStaff)]
	public static bool IsSummonWeapon(ItemIngredient i) => IsInGroup(i, ItemGroup.SummonWeapon);

	[IngredientOption("WeaponFilters", ItemID.FlareGun)]
	public static bool IsClasslessWeapon(ItemIngredient i) => IsWeaponInDamageClass(i, DamageClass.Default);

	[IngredientOption("WeaponFilters")]
	public static IEnumerable<IOptionElement<Predicate<ItemIngredient>>> CustomWeaponFilters(
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
			yield return MakeOptionButton(iconItem.type, name, pred);
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

			yield return MakeOptionButton(icon.type, name, pred);
		}
	}
	#endregion

	#region Item Sorts
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

	private static UIOptionToggleButton<T> MakeOptionButton<T>(int itemID, LocalizedText text,
		T v)
	{
		return new UIOptionToggleButton<T>(v, new UIItemIcon(new(itemID), false)){
			HoverText = text
		};
	}

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
	 * Get filter objects represented by a reflected method. If it matches the delegate type `T`,
	 * then it will be converted to that. If it is a function taking a string (localization key
	 * parent) and returning a sequence of option elements of type `T`, then the result of that
	 * will be used.
	 */
	private static IEnumerable<IOptionElement<T>> GetOptionsFromMethod<T>(MethodInfo m,
		string group)
		where T : Delegate
	{
		var attr = m.GetCustomAttribute<IngredientOptionAttribute>();
		if (attr == null || attr.Group != group) { return []; }

		var pred = TryCreateDelegate<T>(m);
		if (pred != null)
		{
			var text = Language.GetText($"{KeyParent}.{attr.Group}.{m.Name}");
			return [MakeOptionButton(attr.IconID, text, pred)];
		}

		var listGen = TryCreateDelegate<Func<string, IEnumerable<IOptionElement<T>>>>(m);
		if (listGen != null)
		{
			return listGen($"{KeyParent}.{attr.Group}");
		}

		return [];
	}

	private static T? TryCreateDelegate<T>(MethodInfo m) where T : Delegate
	{
		var d = Delegate.CreateDelegate(typeof(T), m, false);
		return d == null ? null : (d as T);
	}

	/*
	 * Convert a predicate to one acting on a less derived type such that the predicate returns
	 * true if and only if the provided value is of the correct type *and* the original predicate
	 * returns true with that value.
	 */
	private static Predicate<U> MakeGenericPredicate<T, U>(Predicate<T> pred) where T : U
	{
		return u => u is T t && pred(t);
	}

	/*
	 * Make a generic version of a comparison, similarly to `MakeGenericPredicate`. The comparison
	 * is performed as follows:
	 * - If the objects are not both of the correct runtime type, then the names of their types
	 *   are compared.
	 * - Otherwise, they are compared with `comp`.
	 */
	private static Comparison<U> MakeGenericComparison<T, U>(Comparison<T> comp) where T : U
	{
		return (x, y) => {
			// Neither should ever be null.
			if (x == null || y == null)
			{
				return -1;
			}
			else if (x is T xt && y is T yt)
			{
				return comp(xt, yt);
			}
			else
			{
				return x.GetType().Name.CompareTo(y.GetType().Name);
			}
		};
	}
}
