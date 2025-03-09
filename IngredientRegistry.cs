using System.Collections.Generic;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;
using System.Linq;
using Terraria.ModLoader;

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
		public required LocalizedText Name;
		public required Func<UIElement> ElementFunc;
		public required OptionRules Rules;

		/*
		 * Localization key for the header above this option. Options with the same key will be
		 * grouped in the same section on the option panel.
		 */
		public string? SectionNameKey = null;
	}

	private class IngredientFilter<T> : AbstractGenericOption<Predicate<IIngredient>>
		where T : IIngredient
	{
		public required Predicate<T> Predicate;
		public override Predicate<IIngredient> Value => i => i is T t && Predicate(t);
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

	// Reset to the default sorts and filters.
	public override void Load()
	{

	}

	public void AddFilter<T>(Predicate<T> pred, int iconItemID, LocalizedText name,
		string? sectionNameKey = null) where T : IIngredient
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
		string? sectionNameKey = null) where T : IIngredient
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

	// Make a sort group element with the sort comparisons for type `T`.
	public UIOptionGroup<Comparison<T>> MakeSortGroup<T>()
		where T : IIngredient
	{
		var opts = _sorts.OfType<IngredientSort<T>>();
		return MakeOptionGroup<Comparison<T>, IngredientSort<T>, Comparison<IIngredient>>(opts,
			f => f.Comparison);
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
			var heading = section[0].SectionNameKey is not null && Language.Exists(section[0].SectionNameKey)
				? Language.GetText(section[0].SectionNameKey)
				: null;
			var subgroup = new UIOptionGroup<T>();

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
