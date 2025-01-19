using System;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;

namespace QuiteEnoughRecipes;

// Various predicates on items used for filtering.
static class ItemPredicates
{
	// Check if `i` is in the creative item group `g`.
	public static bool IsInGroup(Item i, ContentSamples.CreativeHelper.ItemGroup g)
	{
		if (!ContentSamples.ItemCreativeSortingId.TryGetValue(i.type, out var result))
		{
			return false;
		}

		return result.Group == g;
	}

	public static bool IsBlock(Item i) =>
		IsInGroup(i, ContentSamples.CreativeHelper.ItemGroup.Blocks);
	public static bool IsBossSummon(Item i) =>
		IsInGroup(i, ContentSamples.CreativeHelper.ItemGroup.BossItem);

	public static bool IsMeleeWeapon(Item i) =>
		IsInGroup(i, ContentSamples.CreativeHelper.ItemGroup.MeleeWeapon);
	public static bool IsRangedWeapon(Item i) =>
		IsInGroup(i, ContentSamples.CreativeHelper.ItemGroup.RangedWeapon);
	public static bool IsMagicWeapon(Item i) =>
		IsInGroup(i, ContentSamples.CreativeHelper.ItemGroup.MagicWeapon);
	public static bool IsSummonWeapon(Item i) =>
		IsInGroup(i, ContentSamples.CreativeHelper.ItemGroup.SummonWeapon);

	public static bool IsWeaponInDamageClass(Item i, DamageClass dc) =>
		i.CountsAsClass(dc) && !IsTool(i);

	public static bool IsTool(Item i) => i.axe > 0 || i.hammer > 0 || i.pick > 0;
}
