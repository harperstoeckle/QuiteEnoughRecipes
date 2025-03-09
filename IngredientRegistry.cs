using System.Collections.Generic;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;
using System.Linq;

namespace QuiteEnoughRecipes;

/*
 * Used as a general hub for information about ingredients, including full ingredient lists, sorts,
 * and filters.
 */
public static class IngredientRegistry
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

	private static List<AbstractGenericOption<Predicate<IIngredient>>> _filters = [];
	private static List<AbstractGenericOption<Comparison<IIngredient>>> _sorts = [];

	// Map ingredient types to master lists of ingredients of that type.
	private static Dictionary<Type, IGenericValue<IEnumerable<IIngredient>>> _ingredients = new();

	public static void AddFilter<T>(Predicate<T> pred, int iconItemID, LocalizedText name,
		string? sectionNameKey = null) where T : IIngredient
	{
		_filters.Add(new IngredientFilter<T>{
			Predicate = pred,
			Name = name,
			ElementFunc = () => new UIItemIcon(new(iconItemID), false),
			SectionNameKey = sectionNameKey
		});
	}

	public static void AddSort<T>(Comparison<T> comp, int iconItemID, LocalizedText name,
		string? sectionNameKey = null) where T : IIngredient
	{
		_sorts.Add(new IngredientSort<T>{
			Comparison = comp,
			Name = name,
			ElementFunc = () => new UIItemIcon(new(iconItemID), false),
			SectionNameKey = sectionNameKey
		});
	}

	// Add ingredients to the "master list" for a given type.
	public static void AddIngredients<T>(IEnumerable<T> ingredients)
		where T : IIngredient
	{
		if (!_ingredients.TryGetValue(typeof(T), out var list))
		{
			list = new IngredientList<T>(ingredients);
			_ingredients.Add(typeof(T), list);
		}
		else
		{
			(list as IngredientList<T>)?.AddRange(ingredients);
		}
	}
}
