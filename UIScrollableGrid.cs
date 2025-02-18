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
	public void SetDisplayedValue(T value);
}

public class UIScrollableGrid<T, E> : UIElement
	where E : UIElement, IScrollableGridElement<T>, new()
{
	private List<E> _grid = new();
	private List<T> _values = new();
	private float _lastViewPosition = 0;

	// Width and height of grid squares, and padding between squares.
	public float SquareSideLength = 50;
	public float Padding = 5;

	// *all* values to be displayed. These can be scrolled through.
	public List<T> Values
	{
		get => _values;
		set
		{
			_values = value;
			if (Scrollbar != null) { Scrollbar.ViewPosition = 0; }
			SetDisplayedValues();
		}
	}

	public int NumCols { get; private set; } = 0;
	public int NumRows { get; private set; } = 0;

	// If this is not present, scrolling will not happen.
	public UIScrollbar? Scrollbar = null;

	// Offset into the list of values for displaying.
	private int _valuesOffset => (int) ((Scrollbar?.ViewPosition ?? 0) / (SquareSideLength + Padding)) * NumCols;

	public override void Recalculate()
	{
		base.Recalculate();

		if (NumRows <= 0 || NumCols <= 0) { return; }

		int totalRows = (Values.Count + NumCols - 1) / NumCols;

		Scrollbar?.SetView(NumRows * SquareSideLength + (NumRows - 1) * Padding,
			totalRows * SquareSideLength + (totalRows - 1) * Padding);
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
		if (Scrollbar.ViewPosition != _lastViewPosition)
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

		NumCols = (int) ((d.Width + Padding) / (SquareSideLength + Padding));
		NumRows = (int) ((d.Height + Padding) / (SquareSideLength + Padding));

		// Resize the grid without making a whole new list.
		int oldGridCount = _grid.Count;
		int newGridCount = NumRows * NumCols;
		if (oldGridCount > newGridCount)
		{
			_grid.RemoveRange(newGridCount, oldGridCount - newGridCount);
		}
		else if (oldGridCount < newGridCount)
		{
			_grid.AddRange(Enumerable.Repeat(default(E), newGridCount - oldGridCount));
			for (int i = oldGridCount; i < newGridCount; ++i) { _grid[i] = new(); }
		}

		// Extra space added on the top and left to keep the contents centered.
		float leftOffset = (d.Width - NumCols * SquareSideLength - (NumCols - 1) * Padding) / 2;
		float topOffset = 0;

		for (int i = 0; i < NumRows; ++i)
		{
			for (int j = 0; j < NumCols; ++j)
			{
				var elem = _grid[i * NumCols + j];

				elem.Left.Pixels = leftOffset + j * (SquareSideLength + Padding);
				elem.Top.Pixels = topOffset + i * (SquareSideLength + Padding);
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
