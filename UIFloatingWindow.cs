using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using ReLogic.Content;
using System.Linq;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.UI;
using Terraria;
using Terraria.ModLoader.UI;

namespace QuiteEnoughRecipes;

// A traditional floating window that can be dragged and resized.
public class UIFloatingWindow : UIPanel, IWindow
{

	/*
	 * Window resizing considers two different regions. First, the cursor is checked against each
	 * edge of the window. If it is at most `ResizeCornerWidth` from the edge, then that side is
	 * eligible to be resized. E.g., if the cursor is in the `ResizeCornerWidth × ResizeCornerWidth`
	 * region in the top-left of the window, then both the top and left side could be resized.
	 *
	 * However, the cursor must also be within the border of width `ResizeBorderWidth` around the
	 * edge of the window to actually start resizing when clicked.
	 */
	public const float ResizeCornerWidth = 30;
	public const float ResizeBorderWidth = 7;

	/*
	 * Keeps track of screen-space coordinates of the click event that started the drag or resize.
	 * We use screen space coordinates so that dragging can be seamless even if the window changes
	 * parents.
	 */
	private struct DragOrResizeInfo
	{
		public required Vector2 OriginalSize;
		public required Vector2 OriginalPos;
		public required Vector2 OriginalMouse;
	}

	// When not null, we assume this window is being dragged.
	private DragOrResizeInfo? _dragOrResizeInfo = null;

	/*
	 * Each of these is true when the corresponding window resize region is being hovered, or if
	 * we're currently dragging and the resize region was being hovered when we clicked to start
	 * dragging.
	 */
	private bool _resizeLeft = false;
	private bool _resizeRight = false;
	private bool _resizeTop = false;
	private bool _resizeBottom = false;

	private bool HoveringResize => _resizeLeft || _resizeRight || _resizeTop || _resizeBottom;

	public WindowState WindowState { get; private set; } = new();

	/*
	 * When set to false, clicking the mouse on this window will *still* result in the window
	 * acting as if it's being dragged (according to the window manager), but it won't actually
	 * move to follow the cursor.
	 */
	public bool CanDragOrResize = true;

	/*
	 * Mostly used by the tiling window container to detect when a window has been dragged far
	 * enough to be released.
	 */
	public Vector2? DragInitialMousePosition => HoveringResize ? null : _dragOrResizeInfo?.OriginalMouse;

	public UITitleBar TitleBar { get; private set; }

	// Stuff should just be directly appended to this instead of the window itself.
	public UIElement Contents { get; private set; } = new(){
		Width = StyleDimension.Fill,
		Height = new(-UITitleBar.BarHeight, 1),
		VAlign = 1,
	};

	public UIFloatingWindow(LocalizedText title)
	{
		TitleBar = new(title);

		// Just to make sure we don't get tiny windows that are impossible to grab.
		Width = Height = MinWidth = MinHeight = new(150, 0);

		BackgroundColor = QERColors.Browns[3] * 0.7f;
		BorderColor = QERColors.Browns[4] * 0.7f;
		SetPadding(0);
		Contents.SetPadding(ResizeBorderWidth);

		Append(TitleBar);
		Append(Contents);

		var closeButton = new UIQERButton(QERAssets.ButtonClose);
		closeButton.HoverText = Language.GetText("Mods.QuiteEnoughRecipes.UI.CloseHover");
		closeButton.OnLeftClick += (elem, evt) => PressCloseButton();

		TitleBar.AddElement(closeButton);
	}

	public bool IsDraggingOrResizing => _dragOrResizeInfo is not null;

	// This is called when the close button is pressed.
	protected virtual void PressCloseButton() => this.Close();

	/*
	 * We want these to be virtual so it's easy for derived window classes to change their
	 * behavior.
	 *
	 * Ignore mouse interaction while dragging so the mouse can interact with other UI elements
	 * while the window is being dragged (like elements that act as containers for windows).
	 */
	public virtual void OnStartDragging() => IgnoresMouseInteraction = true;
	public virtual void OnStopDragging() => IgnoresMouseInteraction = false;

	public virtual void OnOpen() {}
	public virtual void OnClose() {}
	public virtual void OnWindowManagerLeftMouseUp(UIMouseEvent e) {}
	public virtual void OnWindowManagerRightMouseUp(UIMouseEvent e) {}
	public virtual void OnWindowManagerMiddleMouseUp(UIMouseEvent e) {}

	public override void LeftMouseDown(UIMouseEvent e)
	{
		base.LeftMouseDown(e);
		this.MoveToFront();

		if (e.Target == this || e.Target == Contents || e.Target == TitleBar)
		{
			var dims = GetOuterDimensions();

			var dragState = new DragOrResizeInfo{
				OriginalSize = dims.ToRectangle().Size(),
				OriginalPos = dims.Position(),
				OriginalMouse = Main.MouseScreen,
			};

			/*
			 * If we're not resizing, then we only want to drag the window if we grabbed it by the
			 * top bar.
			 */
			if (HoveringResize || TitleBar.ContainsPoint(Main.MouseScreen))
			{
				_dragOrResizeInfo = dragState;
			}
		}

		// We've started dragging, so we should let the window manager know.
		if (_dragOrResizeInfo is not null && !HoveringResize)
		{
			this.StartDragging();
		}
	}

	public override void LeftMouseUp(UIMouseEvent e)
	{
		base.LeftMouseUp(e);
		_dragOrResizeInfo = null;
		this.StopDragging();
	}

	public override void Update(GameTime t)
	{
		base.Update(t);

		if (IsMouseHovering)
		{
			PlayerInput.LockVanillaMouseScroll("QuiteEnoughRecipes/UIWindow");
			Main.LocalPlayer.mouseInterface = true;
		}

		if (_dragOrResizeInfo is DragOrResizeInfo s)
		{
			if (CanDragOrResize)
			{
				ConvertStyleToAbsolute();

				var offset = Main.MouseScreen - s.OriginalMouse;
				var parentBounds = GetParentDimensions();
				var parentSize = new Vector2(parentBounds.Width, parentBounds.Height);

				/*
				 * Since `_dragOrResizeInfo` is in absolute screen coordinates, we need to make it
				 * relative to the parent.
				 */
				var relativePos = s.OriginalPos - parentBounds.Position();

				// We're not resizing, so we're dragging the window.
				if (!HoveringResize)
				{
					Left.Pixels = MathHelper.Clamp(relativePos.X + offset.X, 0, parentSize.X - Width.Pixels);
					Top.Pixels = MathHelper.Clamp(relativePos.Y + offset.Y, 0, parentSize.Y - Height.Pixels);
				}
				else
				{
					if (_resizeLeft)
					{
						Left.Pixels = MathHelper.Clamp(relativePos.X + offset.X, 0, relativePos.X + s.OriginalSize.X - MinWidth.Pixels);
						Width.Pixels = s.OriginalSize.X + relativePos.X - Left.Pixels;
					}
					else if (_resizeRight)
					{
						Width.Pixels = MathHelper.Clamp(s.OriginalSize.X + offset.X, MinWidth.Pixels, parentSize.X - Left.Pixels);
					}

					if (_resizeTop)
					{
						Top.Pixels = MathHelper.Clamp(relativePos.Y + offset.Y, 0, relativePos.Y + s.OriginalSize.Y - MinHeight.Pixels);
						Height.Pixels = s.OriginalSize.Y + relativePos.Y - Top.Pixels;
					}
					else if (_resizeBottom)
					{
						Height.Pixels = MathHelper.Clamp(s.OriginalSize.Y + offset.Y, MinHeight.Pixels, parentSize.Y - Top.Pixels);
					}
				}

				Recalculate();
			}
		}
		else
		{
			var dims = GetOuterDimensions();

			float right = dims.X + dims.Width;
			float bottom = dims.Y + dims.Height;

			_resizeLeft = Main.mouseX <= dims.X + ResizeCornerWidth;
			_resizeRight = Main.mouseX >= right - ResizeCornerWidth;
			_resizeTop = Main.mouseY <= dims.Y + ResizeCornerWidth;
			_resizeBottom = Main.mouseY >= bottom - ResizeCornerWidth;

			// Cursor is outside of the window border, so we shouldn't be resizing.
			if (dims.X + ResizeBorderWidth < Main.mouseX
					&& Main.mouseX < right - ResizeBorderWidth
					&& dims.Y + ResizeBorderWidth < Main.mouseY
					&& Main.mouseY < bottom - ResizeBorderWidth
					|| !IsMouseHovering)
			{
				_resizeLeft = _resizeRight = _resizeTop = _resizeBottom = false;
			}
		}
	}

	/*
	 * Get rid of all relative values in the style dimensions. This might, for example, be used to
	 * allow a window to change parents while in the middle of dragging it while still maintaining
	 * the same drag behavior.
	 */
	public void ConvertStyleToAbsolute()
	{
		var dims = GetOuterDimensions();
		var parentBounds = GetParentDimensions();
		var relativePos = dims.Position() - parentBounds.Position();

		/*
		 * There is, in my opinion, no clear way to make percentages work with dragging in a
		 * well-behaved way, so we simply convert the dimensions exclusively into pixels once we
		 * start dragging.
		 */
		Left.Pixels = relativePos.X;
		Top.Pixels = relativePos.Y;
		Width.Pixels = dims.Width;
		Height.Pixels = dims.Height;

		Left.Percent = 0;
		Top.Percent = 0;
		Width.Percent = 0;
		Height.Percent = 0;
		HAlign = 0;
		VAlign = 0;

	}

	public CalculatedStyle GetParentDimensions()
	{
		return Parent?.GetInnerDimensions() ?? UserInterface.ActiveInstance.GetDimensions();
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		if (CanDragOrResize)
		{
			// Two sides being resized at once is a corner. One is an edge.
			int numResizeDirs = ((bool[])[_resizeLeft, _resizeRight, _resizeTop, _resizeBottom])
				.Count(b => b);

			if (numResizeDirs == 2)
			{
				if (_resizeLeft && _resizeTop || _resizeBottom && _resizeRight)
				{
					UISystem.CustomCursorTexture = QERAssets.CursorCornerLeft;
				}
				else
				{
					UISystem.CustomCursorTexture = QERAssets.CursorCornerRight;
				}

				UISystem.CustomCursorOffset = UISystem.CustomCursorTexture.Frame().Size() / 2;
			}
			else if (numResizeDirs == 1)
			{
				if (_resizeLeft || _resizeRight)
				{
					UISystem.CustomCursorTexture = QERAssets.CursorEdgeHorizontal;
				}
				else
				{
					UISystem.CustomCursorTexture = QERAssets.CursorEdgeVertical;
				}

				UISystem.CustomCursorOffset = UISystem.CustomCursorTexture.Frame().Size() / 2;
			}
		}

		base.DrawSelf(sb);
	}
}
