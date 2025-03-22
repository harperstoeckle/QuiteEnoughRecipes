using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System;
using Terraria.UI;
using Terraria;
using Terraria.ModLoader;
using Terraria.GameContent.ItemDropRules;

namespace QuiteEnoughRecipes;

/*
 * An `IRecipe` represents anything that might be displayed in the left panel, including things
 * like NPC shops and loot drops.
 */
public interface IRecipe
{
	// UI element that will be displayed in the left panel.
	public UIElement Element { get; }

	/*
	 * List of ingredients in this recipe. Recipes will be sorted by lexicographically comparing
	 * these lists, so ingredients should be listed in an order that will make that work best. In
	 * most cases, this will be from left to right.
	 */
	public IEnumerable<IIngredient> GetIngredients();
}

public class BasicRecipe : IRecipe
{
	public required Item Result;
	public List<Item> RequiredItems = [];
	public List<int> AcceptedGroups = [];
	public List<int> RequiredTiles = [];
	public List<Condition> Conditions = [];
	public Mod? SourceMod;

	public BasicRecipe() {}

	[SetsRequiredMembers]
	public BasicRecipe(Recipe recipe)
	{
		Result = recipe.createItem;
		RequiredItems = recipe.requiredItem;
		AcceptedGroups = recipe.acceptedGroups;
		RequiredTiles = recipe.requiredTile;
		Conditions = recipe.Conditions;
		SourceMod = recipe.Mod;
	}

	public UIElement Element => new UIRecipePanel(Result, RequiredItems, AcceptedGroups,
		RequiredTiles, Conditions, SourceMod);

	public IEnumerable<IIngredient> GetIngredients()
	{
		var groupItems = AcceptedGroups
			.Select(i => RecipeGroup.recipeGroups[i])
			.SelectMany(g => g.ValidItems)
			.Select(i => new Item(i));

		IEnumerable<Item> result = [Result];

		return result
			.Concat(RequiredItems)
			.Concat(groupItems)
			.Select(i => new ItemIngredient(i) as IIngredient);
	}
}

public class NPCShopRecipe : IRecipe
{
	public required AbstractNPCShop Shop;

	public UIElement Element => new UINPCShopPanel(Shop);

	public IEnumerable<IIngredient> GetIngredients()
	{
		IEnumerable<IIngredient> npc = [new NPCIngredient(Shop.NpcType)];
		var items = Shop.ActiveEntries.Select(e => new ItemIngredient(e.Item) as IIngredient);
		return npc.Concat(items);
	}
}

public class ItemDropsRecipe : IRecipe
{
	public required Item Item;
	public required List<DropRateInfo> Drops;

	public UIElement Element => new UIDropsPanel(new UIItemPanel(Item, 70), Drops);

	public IEnumerable<IIngredient> GetIngredients()
	{
		IEnumerable<IIngredient> item = [new ItemIngredient(Item)];
		var drops = Drops.Select(d => new ItemIngredient(new(d.itemId)) as IIngredient);
		return item.Concat(drops);
	}
}

public class NPCDropsRecipe : IRecipe
{
	public required int NPCID;
	public required List<DropRateInfo> Drops;

	public UIElement Element => new UIDropsPanel(new UINPCPanel(NPCID), Drops);

	public IEnumerable<IIngredient> GetIngredients()
	{
		IEnumerable<IIngredient> npc = [new NPCIngredient(NPCID)];
		var drops = Drops.Select(d => new ItemIngredient(new(d.itemId)) as IIngredient);
		return npc.Concat(drops);
	}
}

public class GlobalDropsRecipe : IRecipe
{
	public required List<DropRateInfo> Drops;

	/*
	 * Cheaty way to do this. The "left element" that would usually be a portrait is
	 * empty, so all that's visible is the grid.
	 */
	public UIElement Element => new UIDropsPanel(new UIElement(), Drops);

	/*
	 * TODO: Do something better for global drops. In this current form, searching will do
	 * nothing, but will simply make the recipe disappear when the search doesn't match.
	 */
	public IEnumerable<IIngredient> GetIngredients()
	{
		return Drops.Select(d => new ItemIngredient(new(d.itemId)) as IIngredient);
	}
}
