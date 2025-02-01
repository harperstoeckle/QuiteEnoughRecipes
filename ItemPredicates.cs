using System.Linq;
using System;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;

using ItemGroup = Terraria.ID.ContentSamples.CreativeHelper.ItemGroup;

namespace QuiteEnoughRecipes;

/*
 * Various predicates on items used for filtering. For the most part, I try to stick to the vanilla
 * creative item groups for categorization when possible. This may change in the future if it turns
 * out they're not accurate enough.
 */
static class ItemPredicates
{
	// Check if `i` is in the creative item group `g`.
	public static bool IsInGroup(Item i, params ItemGroup[] groups)
	{
		if (!ContentSamples.ItemCreativeSortingId.TryGetValue(i.type, out var result))
		{
			return false;
		}

		return groups.Any(g => result.Group == g);
	}

	public static bool IsTile(Item i) => IsInGroup(i, ItemGroup.CraftingObjects, ItemGroup.Torches,
		ItemGroup.Wood, ItemGroup.Crates, ItemGroup.PlacableObjects, ItemGroup.Blocks,
		ItemGroup.Rope, ItemGroup.Walls);
	public static bool IsCraftingStation(Item i) => IsInGroup(i, ItemGroup.CraftingObjects);
	public static bool IsLootItem(Item i) => IsInGroup(i, ItemGroup.Crates, ItemGroup.BossBags,
		ItemGroup.GoodieBags);
	public static bool IsBossSummon(Item i) => IsInGroup(i, ItemGroup.BossItem,
		ItemGroup.BossSpawners);
	public static bool IsFishing(Item i) => IsInGroup(i, ItemGroup.FishingRods,
		ItemGroup.FishingQuestFish, ItemGroup.Fish, ItemGroup.FishingBait);
	public static bool IsDye(Item i) => IsInGroup(i, ItemGroup.DyeMaterial, ItemGroup.Dye,
		ItemGroup.HairDye);
	public static bool IsAccessory(Item i) => IsInGroup(i, ItemGroup.Accessories);
	public static bool IsWings(Item i) => i.wingSlot >= 0;
	public static bool IsHook(Item i) => IsInGroup(i, ItemGroup.Hook);
	public static bool IsPet(Item i) => IsInGroup(i, ItemGroup.VanityPet);
	public static bool IsMount(Item i) => IsInGroup(i, ItemGroup.Mount, ItemGroup.Minecart);
	public static bool IsArmor(Item i) => IsInGroup(i, ItemGroup.Headgear, ItemGroup.Torso,
		ItemGroup.Pants) && !IsVanity(i);
	public static bool IsVanity(Item i) => i.vanity;
	public static bool IsPotion(Item i) => IsInGroup(i, ItemGroup.LifePotions,
		ItemGroup.ManaPotions, ItemGroup.BuffPotion, ItemGroup.Flask);
	public static bool IsFood(Item i) => IsInGroup(i, ItemGroup.Food);

	public static bool IsMeleeWeapon(Item i) => IsInGroup(i, ItemGroup.MeleeWeapon);
	public static bool IsRangedWeapon(Item i) => IsInGroup(i, ItemGroup.RangedWeapon);
	public static bool IsMagicWeapon(Item i) => IsInGroup(i, ItemGroup.MagicWeapon);
	public static bool IsSummonWeapon(Item i) => IsInGroup(i, ItemGroup.SummonWeapon);

	public static bool IsClasslessWeapon(Item i) => IsWeaponInDamageClass(i, DamageClass.Default);

	public static bool IsWeaponInDamageClass(Item i, DamageClass dc) =>
		i.CountsAsClass(dc) && IsWeapon(i);

	public static bool IsTool(Item i) => IsInGroup(i, ItemGroup.Pickaxe, ItemGroup.Axe,
		ItemGroup.Hammer);

	public static bool IsWeapon(Item i) => !IsTool(i) && i.damage > 0 && i.ammo == AmmoID.None;
}
