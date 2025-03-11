using System.Collections.Generic;
using System.Linq;
using System;
using Terraria.UI;
using Terraria.GameContent.UI.Elements;

namespace QuiteEnoughRecipes;

// A wrapper around `UIScrollableGrid` that can be queried.
public class UIQueryableIngredientGrid<T, E> : UIElement, IQueryable<T>
	where T : IIngredient
	where E : UIElement, IScrollableGridElement<T>, new()
{
	
	private List<T> _allIngredients;
	private List<T> _filteredIngredients;

	private string _searchText = "";
	private List<Predicate<T>> _filters = [];
	private Comparison<T>? _comparison = null;

	private UIScrollableGrid<T, E> _grid = new();

	public UIQueryableIngredientGrid()
	{
		const float ScrollBarWidth = 30;

		_allIngredients = IngredientRegistry.Instance.GetIngredients<T>();
		_filteredIngredients = new(_allIngredients);

		var scroll = new UIScrollbar();
		scroll.Height.Percent = 1;
		scroll.HAlign = 1;
		scroll.VAlign = 1;

		_grid.Scrollbar = scroll;
		_grid.Values = _filteredIngredients;
		_grid.Width = new StyleDimension(-ScrollBarWidth, 1);
		_grid.Height.Percent = 1;
		_grid.VAlign = 1;

		Append(_grid);
		Append(scroll);
	}

	public void SetSearchText(string text)
	{
		_searchText = text;
		UpdateDisplayedIngredients();
	}

	public void SetFilters(List<Predicate<T>> filters)
	{
		_filters = filters;
		UpdateDisplayedIngredients();
	}

	public void SetSortComparison(Comparison<T>? comparison)
	{
		_comparison = comparison;
		UpdateDisplayedIngredients();
	}

	public IEnumerable<UIFilterGroup<T>> GetFilterGroups()
	{
		return [
			IngredientRegistry.Instance.MakeFilterGroup<T>(),
			IngredientRegistry.Instance.MakeModFilterGroup<T>()
		];
	}

	public IEnumerable<UISortGroup<T>> GetSortGroups()
	{
		return [IngredientRegistry.Instance.MakeSortGroup<T>()];
	}

	// Update what ingredients are being displayed based on the search bar and filters.
	private void UpdateDisplayedIngredients()
	{
		var query = SearchQuery.FromSearchText(_searchText ?? "");

		_filteredIngredients.Clear();
		_filteredIngredients.AddRange(
			_allIngredients.Where(i => query.Matches(i) && (_filters.All(f => f(i)))));

		if (_comparison != null)
		{
			_filteredIngredients.Sort(_comparison);
		}

		_grid.Values = _filteredIngredients;
	}
}
