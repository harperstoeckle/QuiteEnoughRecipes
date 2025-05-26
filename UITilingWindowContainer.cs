using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using Terraria;
using System.Collections.Generic;
using System.Linq;

namespace QuiteEnoughRecipes;

/*
 * Allows windows to be dragged into and out of it. The container is divided horizontally into
 * strips, each of which can support any number of windows stacked vertically. Dragging a window
 * in the middle between two of the strips will create a new strip with that single window.
 * Dragging the window between two stacked windows in the strip will allow that window to be
 * placed between the two windows.
 */
public class UITilingWindowContainer : UIElement
{
	/*
	 * Used to represent a position where the cursor is in the current layout. Used to determine
	 * resizing and new window placement.
	 */
	private interface CursorRegion
	{
		public record None() : CursorRegion;
		public record BetweenStrips(int Index) : CursorRegion;
		public record BetweenWindows(int StripIndex, int Index) : CursorRegion;
	}

	private class StackedWindow
	{
		public required UIFloatingWindow Window;
		public float CurrentHeightPercent = 1;
	}

	// A single strip of stacked windows.
	private class Strip
	{
		public UIElement Container = new(){
			Height = new(0, 1),
		};
		public List<StackedWindow> Windows = new();
		public float CurrentWidthPercent = 1;
	}

	private const float WindowInsertionWidth = 20;

	private List<Strip> _strips = new();
	private UIPanel _previewPanel = new(){
		IgnoresMouseInteraction = true,
		BackgroundColor = Color.White * 0.5f,
		BorderColor = Color.White,
	};

	public UITilingWindowContainer()
	{
		TryInsertWindow(new CursorRegion.BetweenStrips(0), new UIIngredientWindow());
		TryInsertWindow(new CursorRegion.BetweenStrips(0), new UIIngredientWindow());
	}

	public override void Update(GameTime t)
	{
		base.Update(t);

		_previewPanel.Remove();
		ResetToStoredPositions();

		if (!IsMouseHovering) { return; }

		var cursorRegion = GetCursorRegion();

		if (cursorRegion is CursorRegion.BetweenStrips(int i))
		{
			Main.NewText($"BS({i})");
		}
		else if (cursorRegion is CursorRegion.BetweenWindows(int a, int b))
		{
			Main.NewText($"BW({a}, {b})");
		}

		if (UISystem.WindowManager?.JustDropped is UIFloatingWindow droppedWindow)
		{
			droppedWindow.SilentRemove();
			UISystem.WindowManager?.DeferCall(() => TryInsertWindow(cursorRegion, droppedWindow));
		}
		else if (UISystem.WindowManager?.Dragging is UIFloatingWindow)
		{
			TryInsertPreview(cursorRegion);
		}

		//if (RightWindow is not null)
		//{
		//	if (RightWindow.WindowState.WantsClose == CloseRequestState.Close)
		//	{
		//		RightWindow.ConvertStyleToAbsolute();
		//		RightWindow.CanDragOrResize = true;
		//		RightWindow.WindowState.WantsClose = CloseRequestState.None;
		//		RightWindow.OnClose();
		//		RightWindow = null;
		//	}
		//	// Dragged far enough to release it.
		//	else if (RightWindow.DragInitialMousePosition is Vector2 p
		//			&& Vector2.Distance(p, Main.MouseScreen) > 30)
		//	{
		//		RightWindow.ConvertStyleToAbsolute();
		//		RightWindow.CanDragOrResize = true;
		//		UISystem.WindowManager?.Open(RightWindow!);
		//		RightWindow = null;
		//	}
		//}
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		base.DrawSelf(sb);

		//if (_resizeDragBar.IsMouseHovering || _isResizing)
		//{
		//	UISystem.CustomCursorTexture = QERAssets.CursorEdgeHorizontal;
		//	UISystem.CustomCursorOffset = QERAssets.CursorEdgeHorizontal.Frame().Size() / 2;
		//}
	}

	private void ResetToStoredPositions()
	{
		foreach (var s in _strips)
		{
			s.Container.Width.Percent = s.CurrentWidthPercent;
			foreach (var w in s.Windows)
			{
				w.Window.Height.Percent = w.CurrentHeightPercent;
			}
			ArrangeElementsContiguously(s.Windows.Select(w => w.Window), AccessTop, AccessHeight);
		}
		ArrangeElementsContiguously(_strips.Select(s => s.Container), AccessLeft, AccessWidth);
	}

	/*
	 * Insert a window for real, also modifying the stored structure and making the window
	 * undraggable.
	 */
	private void TryInsertWindow(CursorRegion location, UIFloatingWindow window)
	{
		if (location is CursorRegion.None) { return; }

		ResetToStoredPositions();

		window.CanDragOrResize = false;
		var stackedWindow = new StackedWindow{ Window = window };

		window.ConvertStyleToAbsolute();
		window.Width = StyleDimension.Fill;
		window.Height = window.Left = window.Top = StyleDimension.Empty;

		if (location is CursorRegion.BetweenStrips(int i))
		{
			var newStrip = new Strip{ Windows = [stackedWindow] };
			newStrip.Container.Append(stackedWindow.Window);

			if (_strips.Count == 0) { i = 0; }

			InsertElementNormalized(this, _strips.Select(s => s.Container), newStrip.Container,
					i, AccessLeft, AccessWidth);
			_strips.Insert(i, newStrip);

			foreach (var s in _strips)
			{
				s.CurrentWidthPercent = s.Container.Width.Percent;
			}
		}
		else if (location is CursorRegion.BetweenWindows(int stripIndex, int windowIndex))
		{
			InsertElementNormalized(_strips[stripIndex].Container,
					_strips[stripIndex].Windows.Select(w => w.Window), window, windowIndex,
					AccessTop, AccessHeight);
			_strips[stripIndex].Windows.Insert(windowIndex, stackedWindow);

			foreach (var w in _strips[stripIndex].Windows)
			{
				w.CurrentHeightPercent = w.Window.Height.Percent;
			}
		}

		Recalculate();
	}

	// Insert the preview element at the given location.
	private void TryInsertPreview(CursorRegion location)
	{
		_previewPanel.Left = _previewPanel.Top = StyleDimension.Empty;

		if (location is CursorRegion.BetweenStrips(int i))
		{
			if (_strips.Count == 0)
			{
				_previewPanel.Width = StyleDimension.Fill;
				_previewPanel.Height = StyleDimension.Fill;
				Append(_previewPanel);
			}
			else
			{
				_previewPanel.Height = StyleDimension.Fill;
				InsertElementNormalized(this, _strips.Select(s => s.Container), _previewPanel, i,
						AccessLeft, AccessWidth);
			}
		}
		else if (location is CursorRegion.BetweenWindows(int stripIndex, int windowIndex))
		{
			_previewPanel.Width = StyleDimension.Fill;
			InsertElementNormalized(_strips[stripIndex].Container,
					_strips[stripIndex].Windows.Select(w => w.Window), _previewPanel,
					windowIndex, AccessTop, AccessHeight);
		}

		Recalculate();
	}

	delegate ref float DimenAccessor(UIElement e);
	private ref float AccessWidth(UIElement e) => ref e.Width.Precent;
	private ref float AccessHeight(UIElement e) => ref e.Height.Precent;
	private ref float AccessLeft(UIElement e) => ref e.Left.Precent;
	private ref float AccessTop(UIElement e) => ref e.Top.Precent;

	/*
	 * `getPos(e)` gets a reference to the position
	 * `Left` or `Top`, and the size might be `Width` or `Height`. This is included just 
	 */
	private static void InsertElementNormalized(UIElement parent,
			IEnumerable<UIElement> curChildren, UIElement newElement, int index,
			DimenAccessor accessPos, DimenAccessor accessSize)
	{
		var children = curChildren.ToList();
		float newElementWidth = accessSize(newElement) = 1.0f / (children.Count + 1);
		foreach (var c in children) { accessSize(c) *= (1 - newElementWidth); }
		children.Insert(index, newElement);
		parent.Append(newElement);
		ArrangeElementsContiguously(children, accessPos, accessSize);
	}

	private static void ArrangeElementsContiguously(IEnumerable<UIElement> elements,
			DimenAccessor accessPos, DimenAccessor accessSize)
	{
		float offset = 0;
		foreach (var e in elements)
		{
			accessPos(e) = offset;
			offset += accessSize(e);
		}
	}

	/*
	 * If there are no windows, we pretend as if there is a single strip the width of the entire
	 * window.
	 */
	private CursorRegion GetCursorRegion()
	{
		if (!ContainsPoint(Main.MouseScreen)) { return new CursorRegion.None(); }

		var dims = GetInnerDimensions();
		var cursorPercent = (Main.MouseScreen - dims.Position()) / dims.ToRectangle().Size();
		var relativeInsertionWidths = new Vector2(WindowInsertionWidth) / dims.ToRectangle().Size();

		if (_strips.Count == 0)
		{
			int i = RegionIndexFromWidths([1.0f], cursorPercent.X, relativeInsertionWidths.X,
					out bool inbetween);

			return i == -1 || !inbetween
				? new CursorRegion.None()
				: new CursorRegion.BetweenStrips(i);
		}

		{
			int i = RegionIndexFromWidths(_strips.Select(r => r.CurrentWidthPercent),
					cursorPercent.X, relativeInsertionWidths.X, out bool inbetween);

			if (i == -1) { return new CursorRegion.None(); }
			if (inbetween) { return new CursorRegion.BetweenStrips(i); }

			int windowIndex = RegionIndexFromWidths(
					_strips[i].Windows.Select(w => w.CurrentHeightPercent), cursorPercent.Y,
					relativeInsertionWidths.Y, out bool inbetweenWindows);

			return windowIndex == -1 || !inbetweenWindows
				? new CursorRegion.None()
				: new CursorRegion.BetweenWindows(i, windowIndex);
		}
	}

	private static int RegionIndexFromWidths(IEnumerable<float> regions, float pos,
			float inbetweenWidth, out bool inbetween)
	{
		if (pos < -inbetweenWidth / 2)
		{
			inbetween = false;
			return -1;
		}
		else if (pos < inbetweenWidth / 2)
		{
			inbetween = true;
			return 0;
		}

		float offset = 0;

		foreach (var (width, index) in regions.Select((w, i) => (w, i)))
		{
			offset += width;

			if (pos < offset - inbetweenWidth / 2)
			{
				inbetween = false;
				return index;
			}
			else if (pos <= offset + inbetweenWidth / 2)
			{
				inbetween = true;
				return index + 1;
			}
		}

		inbetween = false;
		return -1;
	}
}
