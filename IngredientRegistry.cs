using System.Collections.Generic;
using System.Linq;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

/*
 * Used as a general hub for information about ingredients, including full ingredient lists, sorts,
 * and filters.
 */
public class IngredientRegistry : ModSystem
{
	// Maybe there's a built-in interface for this, but I don't know what it is.
	private interface IGenericValue<T>
	{
		public T Value { get; }
	}

	private abstract class AbstractGenericOption<T> : IGenericValue<T>
	{
		public abstract T Value { get; }

		// For convenience. The specific ingredient type associated with this option.
		public abstract Type IngredientType { get; }

		public required LocalizedText Name;
		public required Func<UIElement> ElementFunc;
		public required OptionRules Rules;

		/*
		 * Localization key for the header above this option. Options with the same key will be
		 * grouped in the same section on the option panel.
		 */
		public string SectionNameKey = "";
	}

	private class IngredientFilter<T> : AbstractGenericOption<Predicate<IIngredient>>
		where T : IIngredient
	{
		public required Predicate<T> Predicate;
		public override Predicate<IIngredient> Value => i => i is T t && Predicate(t);
		public override Type IngredientType => typeof(T);
	}

	private class IngredientSort<T> : AbstractGenericOption<Comparison<IIngredient>>
		where T : IIngredient
	{
		public required Comparison<T> Comparison;
		public override Comparison<IIngredient> Value => (x, y) => {
			if (!(x is T xt && y is T yt))
			{
				return x.GetType().Name.CompareTo(y.GetType().Name);
			}

			return Comparison(xt, yt);
		};
		public override Type IngredientType => typeof(T);
	}

	/*
	 * Normally, it's perfectly possible to convert a `List<T>` to an `IEnumerable<U>` if `T`
	 * derives from `U`. However, this covariance only works if `T` is a class type and not a
	 * struct type. As of now, I have all `IIngredient`s as structs, so they don't work. There's
	 * no good reason not to change them to classes other than my own stubbornness, but this is my
	 * workaround.
	 */
	private class IngredientList<T> : List<T>, IGenericValue<IEnumerable<IIngredient>>
		where T : IIngredient
	{
		public IngredientList(IEnumerable<T> values) : base(values) {}
		public IEnumerable<IIngredient> Value => this.Select(i => i as IIngredient);
	}

	private List<AbstractGenericOption<Predicate<IIngredient>>> _filters = [];
	private List<AbstractGenericOption<Comparison<IIngredient>>> _sorts = [];

	// Map ingredient types to master lists of ingredients of that type.
	private Dictionary<Type, IGenericValue<IEnumerable<IIngredient>>> _ingredients = new();

	public static IngredientRegistry Instance => ModContent.GetInstance<IngredientRegistry>();

	/*
	 * Reset to the default sorts and filters. This is currently done during `PostSetupRecipes` to
	 * ensure that all content from other mods can be properly loaded. However, This should be
	 * designed in such a way that there's a clear time where other mods can add custom filters and
	 * stuff.
	 *
	 * TODO: Is `PostSetupRecipes` the correct option here?
	 */
	public override void PostSetupRecipes()
	{
		_filters.Clear();
		_sorts.Clear();
		_ingredients.Clear();

		var allItems = Enumerable.Range(0, ItemLoader.ItemCount)
			.Select(i => new Item(i))
			.Where(i => i.type != 0)
			.Select(i => new ItemIngredient(i));
		AddIngredients(allItems);

		var allNPCs = Enumerable.Range(0, NPCLoader.NPCCount)
			.Where(n => Main.BestiaryDB.FindEntryByNPCID(n).Icon != null)
			.Select(n => new NPCIngredient(n))
			.ToList();
		AddIngredients(allNPCs);

		var keyParent = "Mods.QuiteEnoughRecipes.OptionGroups";

		var miscItemFilters = IngredientOptions.GetOptionButtons<Predicate<ItemIngredient>>(
			"ItemFilters.Misc");
		foreach (var (pred, icon, name) in miscItemFilters)
		{
			AddFilter<ItemIngredient>(pred, icon, name, $"{keyParent}.ItemFilters.Misc.Name");
		}

		var weaponItemFilters = IngredientOptions.GetOptionButtons<Predicate<ItemIngredient>>(
			"ItemFilters.Weapons");
		foreach (var (pred, icon, name) in weaponItemFilters)
		{
			AddFilter<ItemIngredient>(pred, icon, name, $"{keyParent}.ItemFilters.Weapons.Name");
		}

		var npcFilters = IngredientOptions.GetOptionButtons<Predicate<NPCIngredient>>("NPCFilters");
		foreach (var (pred, icon, name) in npcFilters)
		{
			AddFilter<NPCIngredient>(pred, icon, name, $"{keyParent}.NPCFilters.Name");
		}

		var itemSorts = IngredientOptions.GetOptionButtons<Comparison<ItemIngredient>>(
			"ItemSorts");
		foreach (var (comp, icon, name) in itemSorts)
		{
			AddSort<ItemIngredient>(comp, icon, name, $"{keyParent}.ItemSorts.Name");
		}

		var npcSorts = IngredientOptions.GetOptionButtons<Comparison<NPCIngredient>>("NPCSorts");
		foreach (var (comp, icon, name) in npcSorts)
		{
			AddSort<NPCIngredient>(comp, icon, name, $"{keyParent}.NPCSorts.Name");
		}
	}

	public void AddFilter<T>(Predicate<T> pred, int iconItemID, LocalizedText name,
		string sectionNameKey = "") where T : IIngredient
	{
		_filters.Add(new IngredientFilter<T>{
			Predicate = pred,
			Name = name,
			ElementFunc = () => new UIItemIcon(new(iconItemID), false),
			SectionNameKey = sectionNameKey,
			Rules = OptionRules.AllowDisable | OptionRules.AllowEnable
		});
	}

	public void AddSort<T>(Comparison<T> comp, int iconItemID, LocalizedText name,
		string sectionNameKey = "") where T : IIngredient
	{
		// The first added sort is automatically the default.
		bool shouldBeDefault = !_sorts.Any(s => s is IngredientSort<T>);

		_sorts.Add(new IngredientSort<T>{
			Comparison = comp,
			Name = name,
			ElementFunc = () => new UIItemIcon(new(iconItemID), false),
			SectionNameKey = sectionNameKey,
			Rules = OptionRules.AllowEnable
				| (shouldBeDefault ? OptionRules.EnabledByDefault : 0)
		});
	}

	// Add ingredients to the "master list" for a given type.
	public void AddIngredients<T>(IEnumerable<T> ingredients)
		where T : IIngredient
	{
		if (_ingredients.TryGetValue(typeof(T), out var list))
		{
			(list as IngredientList<T>)?.AddRange(ingredients);
		}
		else
		{
			list = new IngredientList<T>(ingredients);
			_ingredients.Add(typeof(T), list);
		}
	}

	/*
	 * If no ingredient list exists for `T`, an empty list will be returned. Otherwise, this is the
	 * master list of ingredients of type `T`.
	 */
	public List<T> GetIngredients<T>() where T : IIngredient
	{
		_ingredients.TryGetValue(typeof(T), out var list);
		return list as List<T> ?? [];
	}

	// Make a filter group element with the filters for type `T`.
	public UIOptionGroup<Predicate<T>> MakeFilterGroup<T>()
		where T : IIngredient
	{
		var opts = _filters.OfType<IngredientFilter<T>>();
		return MakeOptionGroup<Predicate<T>, IngredientFilter<T>, Predicate<IIngredient>>(opts,
			f => f.Predicate);
	}

	// Get the generic filter group for ingredient type `t`.
	public UIOptionGroup<Predicate<IIngredient>> MakeFilterGroup(Type t)
	{
		var opts =  _filters.Where(f => f.IngredientType == t);
		return MakeOptionGroup<
			Predicate<IIngredient>,
			AbstractGenericOption<Predicate<IIngredient>>,
			Predicate<IIngredient>>(opts, f => f.Value);
	}

	/*
	 * Make a filter group for filtering by mod. Only mods included in the list of ingredients of
	 * type `T` will be used.
	 */
	public UIOptionGroup<Predicate<T>> MakeModFilterGroup<T>()
		where T : IIngredient
	{
		_ingredients.TryGetValue(typeof(T), out var list);
		var mods = (list?.Value ?? []).Select(i => i.Mod)
			.Where(m => m is not null)
			.Select(m => m!);
		return MakeModFilterGroup<T>(mods);
	}

	// Make a generic mod filter group for ingredients of any type in `ingredientTypes`.
	public UIOptionGroup<Predicate<IIngredient>> MakeModFilterGroup(
		IEnumerable<Type> ingredientTypes)
	{
		var mods = ingredientTypes
			.SelectMany(t => _ingredients.TryGetValue(t, out var ings) ? ings.Value : [])
			.Select(i => i.Mod)
			.Where(m => m is not null)
			.Select(m => m!);
		return MakeModFilterGroup<IIngredient>(mods);
	}

	/*
	 * Make a filter group containing each mod in `mods`. Only one filter will be added for each
	 * different mod in `mods`, and they will be in alphabetical order.
	 */
	public UIOptionGroup<Predicate<T>> MakeModFilterGroup<T>(IEnumerable<Mod> mods)
		where T : IIngredient
	{
		var keyParent = "Mods.QuiteEnoughRecipes.OptionGroups.Mods";
		var group = new UIOptionGroup<Predicate<T>>(Language.GetText($"{keyParent}.Name"));

		var sortedMods = mods.Distinct().OrderBy(m => m.DisplayNameClean);
		foreach (var mod in sortedMods)
		{
			var name = Language.GetText($"{keyParent}.ModName")
				.WithFormatArgs(mod.DisplayNameClean);
			var icon = mod.ModSourceBestiaryInfoElement.GetFilterImage();
			var button = new UIOptionToggleButton<Predicate<T>>(i => i.Mod == mod, icon){
				HoverText = name
			};
			group.AddOption(button);
		}

		return group;
	}

	// Make a sort group element with the sort comparisons for type `T`.
	public UIOptionGroup<Comparison<T>> MakeSortGroup<T>()
		where T : IIngredient
	{
		var opts = _sorts.OfType<IngredientSort<T>>();
		return MakeOptionGroup<Comparison<T>, IngredientSort<T>, Comparison<IIngredient>>(opts,
			f => f.Comparison);
	}

	public UIOptionGroup<Comparison<IIngredient>> MakeSortGroup(Type t)
	{
		var opts =  _sorts.Where(f => f.IngredientType == t);
		return MakeOptionGroup<
			Comparison<IIngredient>,
			AbstractGenericOption<Comparison<IIngredient>>,
			Comparison<IIngredient>>(opts, f => f.Value);
	}

	private static UIOptionGroup<T> MakeOptionGroup<T, U, V>(IEnumerable<U> opts, Func<U, T> getValue)
		where U : AbstractGenericOption<V>
	{
		var group = new UIOptionGroup<T>();
		var filterSections = opts
			.GroupBy(f => f.SectionNameKey)
			.Select(g => g.ToList())
			.ToList();

		foreach (var section in filterSections)
		{
			var heading = Language.Exists(section[0].SectionNameKey)
				? Language.GetText(section[0].SectionNameKey)
				: null;
			var subgroup = new UIOptionGroup<T>(heading);

			foreach (var opt in section)
			{
				subgroup.AddOption(
					new UIOptionToggleButton<T>(getValue(opt), opt.ElementFunc(), opt.Rules){
						HoverText = opt.Name
					});
			}

			group.AddSubgroup(subgroup);
		}

		return group;
	}
}
