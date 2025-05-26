using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

// Allows windows to be dragged into and out of it.
public class UITilingWindowContainer : UIElement
{
	private const float ResizeDragBarWidth = 10;

	private UIElement _resizeDragBar = new(){
		Width = new(ResizeDragBarWidth, 0),
		Height = new(0, 1),
		Left = new(-ResizeDragBarWidth / 2, 0.5f),
	};

	private UIElement _leftArea = new(){
		Width = new(-ResizeDragBarWidth / 2, 0.5f),
		Height = new(0, 1),
	};

	private UIElement _rightArea = new(){
		Width = new(-ResizeDragBarWidth / 2, 0.5f),
		Height = new(0, 1),
		HAlign = 1,
	};

	private UIPanel _leftPreview = new(){
		IgnoresMouseInteraction = true,
		BackgroundColor = Color.Transparent,
		BorderColor = Color.Transparent,
		Width = new(0, 1),
		Height = new(0, 1),
	};

	private UIPanel _rightPreview = new(){
		IgnoresMouseInteraction = true,
		BackgroundColor = Color.Transparent,
		BorderColor = Color.Transparent,
		Width = new(0, 1),
		Height = new(0, 1),
	};

	private UIFloatingWindow? _leftWindow = null;
	private UIFloatingWindow? _rightWindow = null;

	bool _isResizing = false;

	// Directly insert left and right windows.
	public UIFloatingWindow? LeftWindow
	{
		get => _leftWindow;
		set
		{
			if (_leftWindow is not null)
			{
				_leftArea.RemoveChild(_leftWindow);
			}

			_leftWindow = value;
			if (_leftWindow is not null)
			{
				_leftWindow.CanDragOrResize = false;
				_leftWindow.Left = _leftWindow.Top = StyleDimension.Empty;
				_leftWindow.Width = _leftWindow.Height = new(0, 1);
				_leftArea.Append(_leftWindow);
				_leftWindow.Recalculate();
			}
		}
	}
	public UIFloatingWindow? RightWindow
	{
		get => _rightWindow;
		set
		{
			if (_rightWindow is not null) { _rightArea.RemoveChild(_rightWindow); }

			_rightWindow = value;
			if (_rightWindow is not null)
			{
				_rightWindow.CanDragOrResize = false;
				_rightWindow.Left = _rightWindow.Top = StyleDimension.Empty;
				_rightWindow.Width = _rightWindow.Height = new(0, 1);
				_rightArea.Append(_rightWindow);
				_rightWindow.Recalculate();

			}
		}
	}

	public UITilingWindowContainer()
	{
		_resizeDragBar.OnLeftMouseDown += (evt, elem) => _isResizing = true;
		_resizeDragBar.OnLeftMouseUp += (evt, elem) => _isResizing = false;

		_leftArea.Append(_leftPreview);
		_rightArea.Append(_rightPreview);
		Append(_resizeDragBar);
		Append(_leftArea);
		Append(_rightArea);
	}

	public override void Update(GameTime t)
	{
		base.Update(t);

		if (_isResizing)
		{
			var dims = GetInnerDimensions();
			var mousePercent = (Main.mouseX - dims.X) / dims.Width;
			mousePercent = Math.Clamp(mousePercent, 0.2f, 0.8f);

			_leftArea.Width = _resizeDragBar.Left = new(-ResizeDragBarWidth / 2, mousePercent);
			_rightArea.Width = new(-ResizeDragBarWidth / 2, 1 - mousePercent);

			Recalculate();
		}

		bool canAcceptLeft = _leftArea.IsMouseHovering && LeftWindow is null;
		bool canAcceptRight = _rightArea.IsMouseHovering && RightWindow is null;

		bool shouldPreviewLeft = canAcceptLeft && UISystem.WindowManager?.Dragging is UIFloatingWindow;
		bool shouldPreviewRight = canAcceptRight && UISystem.WindowManager?.Dragging is UIFloatingWindow;

		_leftPreview.BackgroundColor = shouldPreviewLeft ? Color.White * 0.5f : Color.Transparent;
		_leftPreview.BorderColor = shouldPreviewLeft ? Color.White : Color.Transparent;
		_rightPreview.BackgroundColor = shouldPreviewRight ? Color.White * 0.5f : Color.Transparent;
		_rightPreview.BorderColor = shouldPreviewRight ? Color.White : Color.Transparent;

		if (UISystem.WindowManager?.JustDropped is UIFloatingWindow w)
		{
			if (canAcceptLeft)
			{
				w.SilentRemove();
				UISystem.WindowManager?.DeferCall(() => LeftWindow = w);
			}
			else if (canAcceptRight)
			{
				w.SilentRemove();
				UISystem.WindowManager?.DeferCall(() => RightWindow = w);
			}
		}

		if (LeftWindow is not null)
		{
			if (LeftWindow.WindowState.WantsClose == CloseRequestState.Close)
			{
				LeftWindow.ConvertStyleToAbsolute();
				LeftWindow.CanDragOrResize = true;
				LeftWindow.OnClose();
				LeftWindow = null;
			}
			// Dragged far enough to release it.
			else if (LeftWindow.DragInitialMousePosition is Vector2 p
					&& Vector2.Distance(p, Main.MouseScreen) > 30)
			{
				LeftWindow.ConvertStyleToAbsolute();
				LeftWindow.CanDragOrResize = true;
				UISystem.WindowManager?.Open(LeftWindow!);
				LeftWindow = null;
			}
		}

		if (RightWindow is not null)
		{
			if (RightWindow.WindowState.WantsClose == CloseRequestState.Close)
			{
				RightWindow.ConvertStyleToAbsolute();
				RightWindow.CanDragOrResize = true;
				RightWindow.OnClose();
				RightWindow = null;
			}
			// Dragged far enough to release it.
			else if (RightWindow.DragInitialMousePosition is Vector2 p
					&& Vector2.Distance(p, Main.MouseScreen) > 30)
			{
				RightWindow.ConvertStyleToAbsolute();
				RightWindow.CanDragOrResize = true;
				UISystem.WindowManager?.Open(RightWindow!);
				RightWindow = null;
			}
		}
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		base.DrawSelf(sb);

		if (_resizeDragBar.IsMouseHovering || _isResizing)
		{
			UISystem.CustomCursorTexture = QERAssets.CursorEdgeHorizontal;
			UISystem.CustomCursorOffset = QERAssets.CursorEdgeHorizontal.Frame().Size() / 2;
		}
	}
}
