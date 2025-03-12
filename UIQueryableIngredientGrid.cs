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
	private List<UIFilterGroup<T>> _filterGroups = [
		IngredientRegistry.Instance.MakeFilterGroup<T>(),
		IngredientRegistry.Instance.MakeModFilterGroup<T>()
	];
	private UISortGroup<T> _sortGroup = IngredientRegistry.Instance.MakeSortGroup<T>();

	private UIScrollableGrid<T, E> _grid = new();

	public int TotalResultCount => _allIngredients.Count;
	public int DisplayedResultCount => _filteredIngredients.Count;

	public UIQueryableIngredientGrid()
	{
		const float ScrollBarWidth = 30;

		_allIngredients = IngredientRegistry.Instance.GetIngredients<T>();
		_filteredIngredients = new(_allIngredients);

		foreach (var f in _filterGroups) { f.OnFiltersChanged += UpdateDisplayedIngredients; }
		_sortGroup.OnSortChanged += UpdateDisplayedIngredients;

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

	public IEnumerable<IOptionGroup> GetFilterGroups()
	{
		return _filterGroups;
	}

	public IEnumerable<IOptionGroup> GetSortGroups()
	{
		return [_sortGroup];
	}

	// Update what ingredients are being displayed based on the search bar and filters.
	private void UpdateDisplayedIngredients()
	{
		var query = SearchQuery.FromSearchText(_searchText ?? "");
		var positiveFilters = _filterGroups.SelectMany(f => f.GetPositiveFilters()).ToList();
		var negativeFilters = _filterGroups.SelectMany(f => f.GetNegativeFilters()).ToList();

		var filteredIngredients = _allIngredients
			.Where(i => query.Matches(i)
				&& positiveFilters.All(f => f(i))
				&& !negativeFilters.Any(f => f(i)));

		_filteredIngredients.Clear();
		_filteredIngredients.AddRange(filteredIngredients);

		_filteredIngredients.Sort(_sortGroup.GetActiveSort());

		_grid.Values = _filteredIngredients;
	}
}
