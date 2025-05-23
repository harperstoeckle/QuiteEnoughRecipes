using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

/*
 * A grid element displaying a value of type `T`, and whose value can be swapped out without
 * constructing a new element.
 */
public interface IScrollableGridElement<T>
{
	/*
	 * These will be used to determine the dimensions of the grid. `GridSideLength` should be the
	 * same as the default width of one of these elements when constructed. Note that the grid of
	 * a `UIScrollableGrid` is always square, and elements are placed in the top-left corner.
	 */
	public virtual static int GridSideLength { get; }
	public virtual static int GridPadding { get; }

	public void SetDisplayedValue(T value);
}

public class UIScrollableGrid<T, E> : UIElement
	where E : UIElement, IScrollableGridElement<T>, new()
{
	/*
	 * We arbitrarily choose the minimum number of columns to be the number of columns of the
	 * default size that would fit in a 400-pixel wide grid.
	 */
	private static readonly int MinCols = (int) ((400.0f + E.GridPadding) / (E.GridSideLength + E.GridPadding));

	private List<E> _grid = new();
	private List<T> _values = new();
	private float _lastViewPosition = 0;

	// Width and height of grid squares, and padding between squares.
	private float _squareSideLength = E.GridSideLength;
	private float _padding = E.GridPadding;

	// *all* values to be displayed. These can be scrolled through.
	public List<T> Values
	{
		get => _values;
		set
		{
			_values = value;
			if (Scrollbar != null) { Scrollbar.ViewPosition = 0; }
			Recalculate();
		}
	}

	public int NumCols { get; private set; } = 0;
	public int NumRows { get; private set; } = 0;

	// If this is not present, scrolling will not happen.
	public UIScrollbar? Scrollbar = null;

	// Offset into the list of values for displaying.
	private int _valuesOffset => (int) ((Scrollbar?.ViewPosition ?? 0) / (_squareSideLength + _padding)) * NumCols;

	public override void Recalculate()
	{
		base.Recalculate();

		if (NumRows <= 0 || NumCols <= 0) { return; }

		/*
		 * Even if there are zero total rows, pretend that there is at least one to avoid breaking
		 * the scrollbar.
		 */
		int totalRows = Math.Max((Values.Count + NumCols - 1) / NumCols, 1);

		Scrollbar?.SetView(NumRows * _squareSideLength + (NumRows - 1) * _padding,
			totalRows * _squareSideLength + (totalRows - 1) * _padding);
	}

	public override void RecalculateChildren()
	{
		ReLayoutGrid();
		base.RecalculateChildren();
		SetDisplayedValues();
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		base.DrawSelf(sb);

		/*
		 * This check here is just to avoid re-appending the grid elements every frame even if no
		 * scrolling has happened.
		 */
		if (Scrollbar != null && Scrollbar.ViewPosition != _lastViewPosition)
		{
			SetDisplayedValues();
			_lastViewPosition = Scrollbar.ViewPosition;
		}
	}

	public override void ScrollWheel(UIScrollWheelEvent e)
	{
		base.ScrollWheel(e);
		if (Scrollbar != null)
		{
			Scrollbar.ViewPosition -= e.ScrollWheelValue;
		}
	}

	// Restructure the children based on the current size of this element.
	private void ReLayoutGrid()
	{
		var d = GetInnerDimensions();

		_squareSideLength = E.GridSideLength;
		_padding = E.GridPadding;

		NumCols = (int) ((d.Width + _padding) / (_squareSideLength + _padding));

		// Not enough columns; Make them smaller, but with the same ratio.
		if (NumCols < MinCols)
		{
			float paddingRatio = _padding / _squareSideLength;
			NumCols = MinCols;
			_squareSideLength = d.Width / (NumCols + paddingRatio * NumCols - paddingRatio);
			_padding = _squareSideLength * paddingRatio;
		}

		NumRows = (int) ((d.Height + _padding) / (_squareSideLength + _padding));

		NumCols = Math.Max(0, NumCols);
		NumRows = Math.Max(0, NumRows);

		// Resize the grid without making a whole new list.
		int oldGridCount = _grid.Count;
		int newGridCount = NumRows * NumCols;
		if (oldGridCount > newGridCount)
		{
			_grid.RemoveRange(newGridCount, oldGridCount - newGridCount);
		}
		else if (oldGridCount < newGridCount)
		{
			_grid.AddRange(Enumerable.Range(0, newGridCount - oldGridCount).Select(_ => new E()));
		}

		// Extra space added on the top and left to keep the contents centered.
		float leftOffset = (d.Width - NumCols * _squareSideLength - (NumCols - 1) * _padding) / 2;
		float topOffset = 0;

		for (int i = 0; i < NumRows; ++i)
		{
			for (int j = 0; j < NumCols; ++j)
			{
				var elem = _grid[i * NumCols + j];

				elem.Left.Pixels = leftOffset + j * (_squareSideLength + _padding);
				elem.Top.Pixels = topOffset + i * (_squareSideLength + _padding);
				elem.Width.Pixels = elem.Height.Pixels = _squareSideLength;
			}
		}
	}

	// Set children to the grid elements that should contain a value, and set their values.
	private void SetDisplayedValues()
	{
		int numToShow = Math.Min(Values.Count - _valuesOffset, _grid.Count);

		RemoveAllChildren();
		for (int i = 0; i < _grid.Count; ++i)
		{
			if (i < numToShow)
			{
				_grid[i].SetDisplayedValue(Values[i + _valuesOffset]);
				Append(_grid[i]);
			}
		}
	}
}
