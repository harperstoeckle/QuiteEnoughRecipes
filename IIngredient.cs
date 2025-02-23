using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Terraria.GameContent.Bestiary;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria;

namespace QuiteEnoughRecipes;

/*
 * An ingredient is anything that can participate in a recipe. This obviously includes items, but it
 * can also include things like NPCs (which, for example, are the source in the NPC drops panel).
 */
public interface IIngredient
{
	// These are used for searching.
	public string? Name => null;
	public Mod? Mod => null;
	public IEnumerable<string> GetTooltipLines() => [];

	public bool IsEquivalent(IIngredient other) => false;
}

/*
 * A container of an ingredient, which may also be null. Generally, this should be implemented for
 * UI elements that can be clicked for source or usage information.
 */
public interface IIngredientElement
{
	public IIngredient? Ingredient { get; }
}

public record struct ItemIngredient(Item Item) : IIngredient
{
	public string? Name => Item.Name;
	public Mod? Mod => Item.ModItem?.Mod;

	public IEnumerable<string> GetTooltipLines()
	{
		int yoyoLogo = -1;
		int researchLine = -1;
		int numLines = 1;
		var tooltipNames = new string[30];
		var tooltipLines = new string[30];
		var prefixLines = new bool[30];
		var badPrefixLines = new bool[30];

		Main.MouseText_DrawItemTooltip_GetLinesInfo(Item, ref yoyoLogo, ref researchLine,
			Item.knockBack, ref numLines, tooltipLines, prefixLines, badPrefixLines, tooltipNames,
			out var p);

		var lines = ItemLoader.ModifyTooltips(Item, ref numLines, tooltipNames, ref tooltipLines,
			ref prefixLines, ref badPrefixLines, ref yoyoLogo, out Color?[] o, p);

		/*
		 * We have to remove the item name line (which is the item name, so it's not "part
		 * of the description". We also have to remove any tooltip lines specific to this
		 * mod. Since QER tooltips depend on what slot is being hovered, this can create
		 * weird behavior where hovering over an item in the browser changes search results.
		 */
		return lines
			.Where(l => l.Name != "ItemName" && !(l.Mod is QuiteEnoughRecipes))
			.Select(l => l.Text);
	}

	public bool IsEquivalent(IIngredient other)
	{
		return other is ItemIngredient i && i.Item.type == Item.type;
	}
}

public record struct NPCIngredient(int ID) : IIngredient
{
	public string? Name => Lang.GetNPCNameValue(ID);
	public Mod? Mod => NPCLoader.GetNPC(ID)?.Mod;

	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_key")]
	private extern static ref string FlavorTextBestiaryInfoElement_key(
		FlavorTextBestiaryInfoElement self);

	public IEnumerable<string> GetTooltipLines()
	{
		var elem = Main.BestiaryDB.FindEntryByNPCID(ID).Info
			.OfType<FlavorTextBestiaryInfoElement>()
			.FirstOrDefault();

		if (elem == null) { return []; }

		return [Language.GetTextValue(FlavorTextBestiaryInfoElement_key(elem))];
	}

	public bool IsEquivalent(IIngredient other)
	{
		return other is NPCIngredient n && n.ID == ID;
	}
}
