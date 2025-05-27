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

	private const float WindowInsertionWidth = 40;

	private List<Strip> _strips = new();
	private CursorRegion _cursorRegion = new CursorRegion.None();
	private UIPanel _previewPanel = new(){
		IgnoresMouseInteraction = true,
		BackgroundColor = Color.White * 0.5f,
		BorderColor = Color.White,
	};

	public UITilingWindowContainer()
	{
		TryInsertWindow(new CursorRegion.BetweenStrips(0), UISystem.RecipeWindow!);
		TryInsertWindow(new CursorRegion.BetweenStrips(0), new UIIngredientWindow());
	}

	public override void Update(GameTime t)
	{
		base.Update(t);

		var newCursorRegion = GetCursorRegion();

		if (newCursorRegion != _cursorRegion)
		{
			_previewPanel.Remove();
			ResetToStoredPositions();

			_cursorRegion = newCursorRegion;

			if (UISystem.WindowManager?.Dragging is UIFloatingWindow)
			{
				TryInsertPreview(_cursorRegion);
			}
		}

		if (_cursorRegion is not CursorRegion.None
				&& UISystem.WindowManager?.JustDropped is UIFloatingWindow droppedWindow)
		{
			droppedWindow.SilentRemove();
			UISystem.WindowManager?.DeferCall(() => TryInsertWindow(_cursorRegion, droppedWindow));
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
	private void TryInsertWindow(CursorRegion location, UIFloatingWindow window)
	{
		if (location is CursorRegion.None) { return; }

		window.CanDragOrResize = false;
		var stackedWindow = new StackedWindow{ Window = window };

		window.ConvertStyleToAbsolute();
		window.Width = window.Height = StyleDimension.Fill;
		window.Left = window.Top = StyleDimension.Empty;

		if (location is CursorRegion.BetweenStrips(int i))
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
		else if (location is CursorRegion.BetweenWindows(int stripIndex, int windowIndex))
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
	private void TryInsertPreview(CursorRegion location)
	{
		_previewPanel.Left = _previewPanel.Top = StyleDimension.Empty;

		if (location is CursorRegion.BetweenStrips(int i))
		{
			_previewPanel.Height = StyleDimension.Fill;
			_previewPanel.Width.Percent = _strips.Count == 0 ? 1.0f : 1.0f / _strips.Count;
			Append(_previewPanel);

			var strips = _strips.Select(s => s.Container).ToList<UIElement>();
			strips.Insert(i, _previewPanel);

			ArrangeContiguousNormalized(strips, AccessLeft, AccessWidth);
		}
		else if (location is CursorRegion.BetweenWindows(int stripIndex, int windowIndex))
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
	 * Get the part of the window currently being hovered. If the window is not actively being
	 * hovered, return `CursorRegion.None`.
	 */
	private CursorRegion GetCursorRegion()
	{
		if (!IsMouseHovering) { return new CursorRegion.None(); }

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
