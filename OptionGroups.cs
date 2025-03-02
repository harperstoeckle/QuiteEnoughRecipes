using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria;

namespace QuiteEnoughRecipes;

// Standard QER sorts and filters.
public static class OptionGroups
{
	// Make an option group for mods that are present in a master list of ingredients.
	public static OptionGroup<Predicate<T>> MakeModFilterGroup<T>(IEnumerable<T> ingredients)
		where T : IIngredient
	{
		var keyParent = "Mods.QuiteEnoughRecipes.OptionGroups.Mods";
		var group = new OptionGroup<Predicate<T>>{
			Name = Language.GetText($"{keyParent}.Name")
		};

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
			group.Options.Add(new(icon, name, i => i.Mod == mod));
		}

		return group;
	}

	public static OptionGroup<Predicate<ItemIngredient>> NormalItemFilters()
	{
		IEnumerable<(int, string, Predicate<ItemIngredient>)> filters = [
			(ItemID.StoneBlock, "Tiles", i => ItemPredicates.IsTile(i.Item)),
			(ItemID.Furnace, "CraftingStations", i => ItemPredicates.IsCraftingStation(i.Item)),
			(ItemID.SuspiciousLookingEye, "BossSummons", i => ItemPredicates.IsBossSummon(i.Item)),
			(ItemID.GoodieBag, "LootItems", i => ItemPredicates.IsLootItem(i.Item)),
			(ItemID.InfernoPotion, "Potions", i => ItemPredicates.IsPotion(i.Item)),
			(ItemID.Apple, "Food", i => ItemPredicates.IsFood(i.Item)),
			(ItemID.WoodFishingPole, "Fishing", i => ItemPredicates.IsFishing(i.Item)),
			(ItemID.RedDye, "Dye", i => ItemPredicates.IsDye(i.Item)),
			(ItemID.AnkletoftheWind, "Accessories", i => ItemPredicates.IsAccessory(i.Item)),
			(ItemID.ExoticEasternChewToy, "Pets", i => ItemPredicates.IsPet(i.Item)),
			(ItemID.SlimySaddle, "Mounts", i => ItemPredicates.IsMount(i.Item)),
			(ItemID.CreativeWings, "Wings", i => ItemPredicates.IsWings(i.Item)),
			(ItemID.GrapplingHook, "Hooks", i => ItemPredicates.IsHook(i.Item)),
			(ItemID.CopperPickaxe, "Tools", i => ItemPredicates.IsTool(i.Item)),
			(ItemID.CopperChainmail, "Armor", i => ItemPredicates.IsArmor(i.Item)),
			(ItemID.RedHat, "Vanity", i => ItemPredicates.IsVanity(i.Item)),
		];

		var keyParent = "Mods.QuiteEnoughRecipes.OptionGroups.ItemFilters";

		return MakeOptionGroup(filters, keyParent);
	}

	public static OptionGroup<Predicate<ItemIngredient>> WeaponItemFilters()
	{
		var keyParent = "Mods.QuiteEnoughRecipes.OptionGroups.WeaponFilters";
		IEnumerable<(int, string, Predicate<ItemIngredient>)> damageFilters = [
			(ItemID.CopperShortsword, "MeleeWeapons", i => ItemPredicates.IsMeleeWeapon(i.Item)),
			(ItemID.WoodenBow, "RangedWeapons", i => ItemPredicates.IsRangedWeapon(i.Item)),
			(ItemID.WandofSparking, "MagicWeapons", i => ItemPredicates.IsMagicWeapon(i.Item)),
			(ItemID.BabyBirdStaff, "SummonWeapons", i => ItemPredicates.IsSummonWeapon(i.Item)),
			(ItemID.FlareGun, "ClasslessWeapons", i => ItemPredicates.IsClasslessWeapon(i.Item))
		];

		var group = MakeOptionGroup(damageFilters, keyParent);

		// We only add the thrower class filter if there are mods that add throwing weapons.
		if (FindIconItemForDamageClass(DamageClass.Throwing) is Item iconItem)
		{
			/*
			 * Unlike the other modded damage classes, we only want to show throwing weapons that
			 * are *exactly* in the throwing class, since modded classes that just *derive* from
			 * throwing (like rogue) will also be shown below with the other modded classes.
			 */
			group.Options.Add(
				new(new UIItemIcon(iconItem, false), Language.GetText($"{keyParent}.ThrowingWeapons"),
					i => i.Item.DamageType == DamageClass.Throwing && ItemPredicates.IsWeapon(i.Item)));
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

			// Adjust the name so instead of "rogue damage Weapons", we get "Rogue Weapons".
			var name = Language.GetText($"{keyParent}.OtherWeapons")
				.WithFormatArgs(BaseDamageClassName(dcs[0].DisplayName.Value));
			group.Options.Add(
				new(new UIItemIcon(icon, false), name,
					i => dcs.Any(dc => ItemPredicates.IsWeaponInDamageClass(i.Item, dc))));
		}

		return group;
	}

	public static OptionGroup<Comparison<ItemIngredient>> ItemSorts()
	{
		IEnumerable<(int, string, Comparison<ItemIngredient>)> sorts = [
			(ItemID.AlphabetStatue1, "ID", (x, y) => x.Item.type.CompareTo(y.Item.type)),
			(ItemID.AlphabetStatueA, "Alphabetical", (x, y) => x.Name.CompareTo(y.Name)),
			(ItemID.StarStatue, "Rarity", (x, y) => x.Item.rare.CompareTo(y.Item.rare)),
			(ItemID.ChestStatue, "Value", (x, y) => x.Item.value.CompareTo(y.Item.value))
		];

		var keyParent = "Mods.QuiteEnoughRecipes.OptionGroups.ItemSorts";
		return MakeOptionGroup(sorts, keyParent);
	}

	public static OptionGroup<Predicate<NPCIngredient>> NPCFilters()
	{
		IEnumerable<(int, string, Predicate<NPCIngredient>)> filters = [
			(ItemID.SlimeCrown, "Bosses",
				n => TryGetBestiaryInfoElement<BossBestiaryInfoElement>(n.ID) != null),
			(ItemID.GoldCoin, "TownNPCs", n => TryGetNPC(n.ID)?.isLikeATownNPC ?? false)
		];

		var keyParent = "Mods.QuiteEnoughRecipes.OptionGroups.NPCFilters";
		return MakeOptionGroup(filters, keyParent);
	}

	public static OptionGroup<Comparison<NPCIngredient>> NPCSorts()
	{
		IEnumerable<(int, string, Comparison<NPCIngredient>)> sorts = [
			(ItemID.AlphabetStatue1, "ID", (x, y) => x.ID.CompareTo(y.ID)),
			(ItemID.AlphabetStatueA, "Alphabetical", (x, y) => x.Name.CompareTo(y.Name)),
			(ItemID.StarStatue, "Rarity", (x, y) => {
				int xRare = TryGetNPC(x.ID)?.rarity ?? 0;
				int yRare = TryGetNPC(y.ID)?.rarity ?? 0;
				return xRare.CompareTo(yRare);
			})
		];

		var keyParent = "Mods.QuiteEnoughRecipes.OptionGroups.NPCSorts";
		return MakeOptionGroup(sorts, keyParent);
	}

	private static OptionGroup<T> MakeOptionGroup<T>(IEnumerable<(int, string, T)> opts,
		string keyParent)
	{
		var group = new OptionGroup<T>();
		if (Language.Exists($"{keyParent}.Name"))
		{
			group.Name = Language.GetText($"{keyParent}.Name");
		}

		foreach (var (id, key, v) in opts)
		{
			var icon = new UIItemIcon(new(id), false);
			group.Options.Add(new(icon, Language.GetText($"{keyParent}.{key}"), v));
		}

		return group;
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
			.Where(i => i.DamageType == d && ItemPredicates.IsWeapon(i))
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
}
