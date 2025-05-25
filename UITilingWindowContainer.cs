using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
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
		Left = new(ResizeDragBarWidth / 2, 0.5f),
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

	public UITilingWindowContainer()
	{
		_leftArea.Append(_leftPreview);
		_rightArea.Append(_rightPreview);
		Append(_resizeDragBar);
		Append(_leftArea);
		Append(_rightArea);
	}

	public override void Update(GameTime t)
	{
		base.Update(t);

		bool canAcceptLeft = _leftArea.IsMouseHovering && _leftWindow is null;
		bool canAcceptRight = _rightArea.IsMouseHovering && _rightWindow is null;

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
				_leftWindow = w;
				w.ReparentTo(_leftArea);

				UISystem.WindowManager?.DeferCall(
						() => {
							w.CanDragOrResize = false;
							w.Left = w.Top = StyleDimension.Empty;
							w.Width = w.Height = new(0, 1);
						});
			}
			else if (canAcceptRight)
			{
				_rightWindow = w;
				w.ReparentTo(_rightArea);
				UISystem.WindowManager?.DeferCall(
						() => {
							w.CanDragOrResize = false;
							w.Left = w.Top = StyleDimension.Empty;
							w.Width = w.Height = new(0, 1);
						});
			}
		}

		if (_leftWindow is not null)
		{
			if (_leftWindow.WindowState.WantsClose)
			{
				_leftWindow.CanDragOrResize = true;
				_leftArea.RemoveChild(_leftWindow);
				_leftWindow = null;
			}
			// Dragged far enough to release it.
			else if (_leftWindow.DragInitialMousePosition is Vector2 p
					&& Vector2.Distance(p, Main.MouseScreen) > 30)
			{
				_leftWindow.ConvertStyleToAbsolute();
				_leftWindow.CanDragOrResize = true;
				_leftArea.RemoveChild(_leftWindow);
				UISystem.WindowManager?.Open(_leftWindow!);
				_leftWindow = null;
			}
		}

		if (_rightWindow is not null)
		{
			if (_rightWindow.WindowState.WantsClose)
			{
				_rightWindow.CanDragOrResize = true;
				_rightArea.RemoveChild(_rightWindow);
				_rightWindow = null;
			}
			// Dragged far enough to release it.
			else if (_rightWindow.DragInitialMousePosition is Vector2 p
					&& Vector2.Distance(p, Main.MouseScreen) > 30)
			{
				_rightWindow.ConvertStyleToAbsolute();
				_rightWindow.CanDragOrResize = true;
				_rightArea.RemoveChild(_rightWindow);
				UISystem.WindowManager?.Open(_rightWindow!);
				_rightWindow = null;
			}
		}
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		base.DrawSelf(sb);

		if (_resizeDragBar.IsMouseHovering)
		{
			UISystem.CustomCursorTexture = QERAssets.CursorEdgeHorizontal;
			UISystem.CustomCursorOffset = QERAssets.CursorEdgeHorizontal.Frame().Size() / 2;
		}
	}
}
