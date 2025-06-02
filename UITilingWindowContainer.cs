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
	 * Represents an "edge" in the layout (i.e., somewhere where a new window could be placed).
	 * This could either be in between strips (or at the left or right edge of the container) or
	 * between windows in one of the strips.
	 */
	private interface LayoutEdge
	{
		public record None() : LayoutEdge;
		public record BetweenStrips(int Index) : LayoutEdge;
		public record BetweenWindows(int StripIndex, int Index) : LayoutEdge;
	}

	private struct CursorRegion
	{
		/*
		 * Edge the mouse is hovering. If the user is dragging a floating window, then this is
		 * where the window would be placed. The "line" that needs to be hovered is of thickness
		 * `WindowInsertionWidth`.
		 */
		public LayoutEdge LayoutEdge = new LayoutEdge.None();

		/*
		 * The window insertion area is pretty wide to make it easy to insert windows, to the
		 * extent that it bleeds slightly into the actual content of the windows. If we were to
		 * allow windows to be resized from anywhere in that region, then some UI elements near the
		 * edge of the window would no longer work, since clicking would resize instead. So we say
		 * that the mouse is in the resize area if it's in the strip of width `ResizeWidth` in the
		 * middle of the edge area.
		 */
		public bool IsInResizeArea = false;

		public CursorRegion() {}
	}

	private class StackedWindow
	{
		public required UIFloatingWindow Window;
		public float CurrentHeightPercent = 1;
		public bool ShouldRemove = false;

		public required StyleDimension WidthBeforeInsertion;
		public required StyleDimension HeightBeforeInsertion;
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

	private const float WindowInsertionWidth = 60;
	private const float ResizeWidth = 2 * UIFloatingWindow.ResizeBorderWidth;

	private List<Strip> _strips = new();
	private CursorRegion _cursorRegion = new();
	private bool _isResizing = false;
	private UIPanel _previewPanel = new(){
		IgnoresMouseInteraction = true,
		BackgroundColor = Color.White * 0.5f,
		BorderColor = Color.White,
	};

	public UITilingWindowContainer()
	{
		TryInsertWindow(new LayoutEdge.BetweenStrips(0), UISystem.RecipeWindow!);
		TryInsertWindow(new LayoutEdge.BetweenStrips(0), new UIIngredientWindow());
	}

	public override void Update(GameTime t)
	{
		base.Update(t);

		bool didChangeLayout = false;

		// Remove closed or dragged windows.
		foreach (var s in _strips)
		{
			foreach (var w in s.Windows)
			{
				if (w.Window.WindowState.WantsClose == CloseRequestState.Close)
				{
					w.Window.ConvertStyleToAbsolute();
					w.Window.CanDragOrResize = true;
					w.Window.WindowState.WantsClose = CloseRequestState.None;
					w.Window.OnClose();
					w.Window.Remove();
					w.Window.Width = w.WidthBeforeInsertion;
					w.Window.Height = w.HeightBeforeInsertion;
					w.ShouldRemove = true;
				}
				else if (w.Window.DragInitialMousePosition is Vector2 p
						&& Vector2.Distance(p, Main.MouseScreen) > 30)
				{
					w.Window.ConvertStyleToAbsolute();
					w.Window.CanDragOrResize = true;
					w.Window.Remove();
					UISystem.WindowManager?.Open(w.Window);
					UISystem.WindowManager?.DeferCall(
							() => {
								w.Window.Width = w.WidthBeforeInsertion;
								w.Window.Height = w.HeightBeforeInsertion;

								/*
								 * Since floating windows use their cached positions and sizes to
								 * handle dragging, we need to recalculate here so it's actually
								 * using the right ones. Otherwise, it will just use the stored size
								 * from when it was in this container.
								 */
								w.Window.Recalculate();
							});
					w.ShouldRemove = true;
				}
			}

			if (s.Windows.RemoveAll(w => w.ShouldRemove) > 0)
			{
				ArrangeContiguousNormalized(s.Windows, w => ref w.Window.Top.Precent,
						w => ref w.CurrentHeightPercent);
				didChangeLayout = true;
			}

			if (s.Windows.Count == 0)
			{
				s.Container.Remove();
			}
		}

		if (_strips.RemoveAll(s => s.Windows.Count == 0) > 0)
		{
			ArrangeContiguousNormalized(_strips, s => ref s.Container.Left.Precent,
					s => ref s.CurrentWidthPercent);
			didChangeLayout = true;
		}

		/*
		 * Changing layout means the stored region being resized could no longer be meaningful.
		 * But this should not be able to happen.
		 */
		if (didChangeLayout)
		{
			ResetToStoredPositions();
			_isResizing = false;
		}

		if (_isResizing)
		{
			var dims = GetInnerDimensions();
			var cursorPos = GetMouseAsPercent();

			if (_cursorRegion.LayoutEdge is LayoutEdge.BetweenStrips(int i))
			{
				Func<Strip, float> getMinStripSize = s => {
					if (s.Windows.Count == 0) { return 0; }
					else
					{
						return s.Windows.Select(w => GetPercent(w.Window.MinWidth, dims.Width)).Max();
					}
				};

				ResizeFromCursorPos(_strips, i, cursorPos.X, s => ref s.CurrentWidthPercent, getMinStripSize);
			}
			else if (_cursorRegion.LayoutEdge is LayoutEdge.BetweenWindows(int stripIndex, int windowIndex))
			{
				ResizeFromCursorPos(_strips[stripIndex].Windows, windowIndex, cursorPos.Y,
						w => ref w.CurrentHeightPercent, w => GetPercent(w.Window.MinHeight, dims.Height));
			}

			ResetToStoredPositions();
			return;
		}

		var newCursorRegion = GetCursorRegion();

		if (newCursorRegion.LayoutEdge != _cursorRegion.LayoutEdge)
		{
			_previewPanel.Remove();
			ResetToStoredPositions();

			if (UISystem.WindowManager?.Dragging is UIFloatingWindow)
			{
				TryInsertPreview(newCursorRegion.LayoutEdge);
			}
		}

		if (newCursorRegion.LayoutEdge is not LayoutEdge.None
				&& UISystem.WindowManager?.JustDropped is UIFloatingWindow droppedWindow)
		{
			droppedWindow.SilentRemove();
			UISystem.WindowManager?.DeferCall(() => TryInsertWindow(newCursorRegion.LayoutEdge, droppedWindow));
		}

		_cursorRegion = newCursorRegion;
	}

	public override void LeftMouseDown(UIMouseEvent e)
	{
		base.LeftMouseDown(e);

		if (_cursorRegion.IsInResizeArea
				&& (_cursorRegion.LayoutEdge is LayoutEdge.BetweenStrips(int i)
					&& 0 < i && i < _strips.Count
					|| _cursorRegion.LayoutEdge is LayoutEdge.BetweenWindows(int stripIndex, int windowIndex)
					&& 0 < windowIndex && windowIndex < _strips[stripIndex].Windows.Count))
		{
			_isResizing = true;
		}
	}

	public override void LeftMouseUp(UIMouseEvent e)
	{
		base.LeftMouseUp(e);
		_isResizing = false;
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		base.DrawSelf(sb);

		if (_cursorRegion.IsInResizeArea
				&& _cursorRegion.LayoutEdge is LayoutEdge.BetweenStrips(int i)
				&& 0 < i && i < _strips.Count)
		{
			UISystem.CustomCursorTexture = QERAssets.CursorEdgeHorizontal;
			UISystem.CustomCursorOffset = QERAssets.CursorEdgeHorizontal.Frame().Size() / 2;
		}
		else if (_cursorRegion.IsInResizeArea
				&& _cursorRegion.LayoutEdge is LayoutEdge.BetweenWindows(int stripIndex, int windowIndex)
				&& 0 < windowIndex && windowIndex < _strips[stripIndex].Windows.Count)
		{
			UISystem.CustomCursorTexture = QERAssets.CursorEdgeVertical;
			UISystem.CustomCursorOffset = QERAssets.CursorEdgeVertical.Frame().Size() / 2;
		}
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

			ArrangeContiguousNormalized(s.Windows, w => ref w.Window.Top.Precent,
					w => ref w.Window.Height.Precent);
		}

		ArrangeContiguousNormalized(_strips, s => ref s.Container.Left.Precent,
				s => ref s.Container.Width.Precent);

		Recalculate();
	}

	/*
	 * Insert a window for real, also modifying the stored structure and making the window
	 * undraggable.
	 */
	private void TryInsertWindow(LayoutEdge location, UIFloatingWindow window)
	{
		if (location is LayoutEdge.None) { return; }

		window.CanDragOrResize = false;
		var stackedWindow = new StackedWindow{
			Window = window,
			WidthBeforeInsertion = window.Width,
			HeightBeforeInsertion = window.Height,
		};

		window.ConvertStyleToAbsolute();
		window.Width = window.Height = StyleDimension.Fill;
		window.Left = window.Top = StyleDimension.Empty;

		if (location is LayoutEdge.BetweenStrips(int i))
		{
			if (_strips.Count == 0) { i = 0; }

			var newStrip = new Strip{ Windows = [stackedWindow] };
			newStrip.CurrentWidthPercent = _strips.Count == 0 ? 1.0f : 1.0f / _strips.Count;
			newStrip.Container.Height = StyleDimension.Fill;
			newStrip.Container.Append(stackedWindow.Window);
			Append(newStrip.Container);

			_strips.Insert(i, newStrip);

			ArrangeContiguousNormalized(_strips, s => ref s.Container.Left.Precent,
					s => ref s.CurrentWidthPercent);
		}
		else if (location is LayoutEdge.BetweenWindows(int stripIndex, int windowIndex))
		{
			var windows = _strips[stripIndex].Windows;
			stackedWindow.CurrentHeightPercent = windows.Count == 0 ? 1.0f : 1.0f / windows.Count;
			_strips[stripIndex].Container.Append(window);
			windows.Insert(windowIndex, stackedWindow);

			ArrangeContiguousNormalized(windows, w => ref w.Window.Top.Precent,
					w => ref w.CurrentHeightPercent);
		}

		ResetToStoredPositions();
	}

	// Insert the preview element at the given location.
	private void TryInsertPreview(LayoutEdge location)
	{
		_previewPanel.Left = _previewPanel.Top = StyleDimension.Empty;

		if (location is LayoutEdge.BetweenStrips(int i))
		{
			if (_strips.Count == 0) { i = 0; }

			_previewPanel.Height = StyleDimension.Fill;
			_previewPanel.Width.Percent = _strips.Count == 0 ? 1.0f : 1.0f / _strips.Count;
			Append(_previewPanel);

			var strips = _strips.Select(s => s.Container).ToList<UIElement>();
			strips.Insert(i, _previewPanel);

			ArrangeContiguousNormalized(strips, AccessLeft, AccessWidth);
		}
		else if (location is LayoutEdge.BetweenWindows(int stripIndex, int windowIndex))
		{
			var windows = _strips[stripIndex].Windows;
			_previewPanel.Width = StyleDimension.Fill;
			_previewPanel.Height.Percent = windows.Count == 0 ? 1.0f : 1.0f / windows.Count;

			_strips[stripIndex].Container.Append(_previewPanel);

			var elements = windows.Select(w => w.Window).ToList<UIElement>();
			elements.Insert(windowIndex, _previewPanel);

			ArrangeContiguousNormalized(elements, AccessTop, AccessHeight);
		}

		Recalculate();
	}

	delegate ref U Accessor<T, U>(T t);
	private ref float AccessWidth(UIElement e) => ref e.Width.Precent;
	private ref float AccessHeight(UIElement e) => ref e.Height.Precent;
	private ref float AccessLeft(UIElement e) => ref e.Left.Precent;
	private ref float AccessTop(UIElement e) => ref e.Top.Precent;

	/*
	 * Modify positions and sizes of each element in `elements` such that their total size adds up
	 * to 1 and they are laid out end-to-end. Their sizes will remain the same relative to each
	 * other.
	 */
	private static void ArrangeContiguousNormalized<T>(List<T> elements,
			Accessor<T, float> accessPos, Accessor<T, float> accessSize)
	{
		float curTotalSize = elements.Select(e => accessSize(e)).Sum();

		float offset = 0;
		foreach (var e in elements)
		{
			accessSize(e) /= curTotalSize;
			accessPos(e) = offset;
			offset += accessSize(e);
		}
	}

	/*
	 * `elements` is a list of element-like objects whose sizes (on one axis) can be accessed with
	 * `accessSize`. `barIndex` is the index of the "bar" (area between elements) being grabbed,
	 * and `cursorPos` is the position of the cursor on that axis. This function will resize the
	 * elements left and right of the bar at that index to match the mouse position (clamped if
	 * the mouse moved too far and the sizes would be smaller than the min sizes obtained with
	 * `getMinSize`). All dimensions should be given as fractions of the container size.
	 */
	private static void ResizeFromCursorPos<T>(List<T> elements, int barIndex, float cursorPos,
			Accessor<T, float> accessSize, Func<T, float> getMinSize)
	{
		if (barIndex <= 0 || barIndex >= elements.Count) { return; }

		float leftOffset = elements.Take(barIndex - 1).Select(e => accessSize(e)).Sum();
		float rightOffset = leftOffset + accessSize(elements[barIndex - 1]) + accessSize(elements[barIndex]);

		float minPos = leftOffset + getMinSize(elements[barIndex - 1]);
		float maxPos = rightOffset - getMinSize(elements[barIndex]);

		// Windows are too small to resize.
		if (maxPos <= minPos) { return; }

		float newBarPos = Math.Clamp(cursorPos, minPos, maxPos);
		accessSize(elements[barIndex - 1]) = newBarPos - leftOffset;
		accessSize(elements[barIndex]) = rightOffset - newBarPos;
	}

	/*
	 * Get the part of the window currently being hovered. If the window is not actively being
	 * hovered, return `CursorRegion.None`.
	 */
	private CursorRegion GetCursorRegion()
	{
		if (!IsMouseHovering) { return new CursorRegion(); }

		var cursorPercent = GetMouseAsPercent();
		var dims = GetInnerDimensions();
		var relativeInsertionWidths = new Vector2(WindowInsertionWidth) / dims.ToRectangle().Size();
		var relativeResizeWidths = new Vector2(ResizeWidth) / dims.ToRectangle().Size();

		if (_strips.Count == 0)
		{
			int i = RegionIndexFromWidths([1.0f], cursorPercent.X, relativeInsertionWidths.X,
					relativeResizeWidths.X, out bool inbetween, out bool inResize);

			LayoutEdge layoutEdge = i == -1 || !inbetween
				? new LayoutEdge.None()
				: new LayoutEdge.BetweenStrips(0);

			return new CursorRegion{ LayoutEdge = layoutEdge, IsInResizeArea = inResize };
		}

		{
			int i = RegionIndexFromWidths(_strips.Select(r => r.CurrentWidthPercent),
					cursorPercent.X, relativeInsertionWidths.X, relativeResizeWidths.X,
					out bool inbetween, out bool inResize);

			if (i == -1) { return new CursorRegion(); }
			if (inbetween)
			{
				return new CursorRegion{
					LayoutEdge = new LayoutEdge.BetweenStrips(i),
					IsInResizeArea = inResize,
				};
			}

			int windowIndex = RegionIndexFromWidths(
					_strips[i].Windows.Select(w => w.CurrentHeightPercent), cursorPercent.Y,
					relativeInsertionWidths.Y, relativeResizeWidths.Y,
					out bool inbetweenWindows, out bool inResizeBetweenWindows);

			LayoutEdge layoutEdge = windowIndex == -1 || !inbetweenWindows
				? new LayoutEdge.None()
				: new LayoutEdge.BetweenWindows(i, windowIndex);

			return new CursorRegion{ LayoutEdge = layoutEdge, IsInResizeArea = inResizeBetweenWindows };
		}
	}

	/*
	 * Mouse coordinates as a percentage of the size of the inner dimensions of this element,
	 * relative to the top-left corner.
	 */
	private Vector2 GetMouseAsPercent()
	{
		var dims = GetInnerDimensions();
		return (Main.MouseScreen - dims.Position()) / dims.ToRectangle().Size();
	}

	private static int RegionIndexFromWidths(IEnumerable<float> regions, float pos,
			float inbetweenWidth, float resizeWidth, out bool inbetween, out bool inResize)
	{
		if (pos < -inbetweenWidth / 2)
		{
			inbetween = false;
			inResize = false;
			return -1;
		}
		else if (pos < inbetweenWidth / 2)
		{
			inbetween = true;
			inResize = -resizeWidth / 2 <= pos && pos <= resizeWidth / 2;
			return 0;
		}

		float offset = 0;

		foreach (var (width, index) in regions.Select((w, i) => (w, i)))
		{
			offset += width;

			if (pos < offset - inbetweenWidth / 2)
			{
				inbetween = false;
				inResize = false;
				return index;
			}
			else if (pos <= offset + inbetweenWidth / 2)
			{
				inbetween = true;
				inResize = offset - resizeWidth / 2 <= pos && pos <= offset + resizeWidth / 2;
				return index + 1;
			}
		}

		inResize = false;
		inbetween = false;
		return -1;
	}

	/*
	 * Get a child style dimension as a percent of the parent size. Like `GetValue`, but for percent
	 * instead. `parentSize` is in pixels.
	 */
	private static float GetPercent(StyleDimension childSize, float parentSize)
	{
		return childSize.Percent + childSize.Pixels / parentSize;
	}
}
