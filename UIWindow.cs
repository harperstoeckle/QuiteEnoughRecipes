using Microsoft.Xna.Framework;
using System.Linq;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.UI;
using Terraria;
using ReLogic.Content;
using Microsoft.Xna.Framework.Graphics;

namespace QuiteEnoughRecipes;

public class UIWindow : UIPanel
{
	private const float BarHeight = 30;

	/*
	 * Window resizing considers two different regions. First, the cursor is checked against each
	 * edge of the window. If it is at most `ResizeCornerWidth` from the edge, then that side is
	 * eligible to be resized. E.g., if the cursor is in the `ResizeCornerWidth × ResizeCornerWidth`
	 * region in the top-left of the window, then both the top and left side could be resized.
	 *
	 * However, the cursor must also be within the border of width `ResizeBorderWidth` around the
	 * edge of the window to actually start resizing when clicked.
	 */
	private const float ResizeCornerWidth = 30;
	private const float ResizeBorderWidth = 7;

	private static readonly Color BarColor = new Color(63, 82, 151) * 0.7f;

	private struct DragState
	{
		public required Vector2 OriginalSize;
		public required Vector2 OriginalPos;
		public required Vector2 OriginalMouse;
	}

	private UIPanel _topBar = new(){
		Width = StyleDimension.Fill,
		Height = new(BarHeight, 0),
		IgnoresMouseInteraction = true,
		BackgroundColor = BarColor,
		BorderColor = BarColor,
	};

	private Asset<Texture2D> _cursorCornerRight = QuiteEnoughRecipes.Instance.Assets.Request<Texture2D>("Images/cursor_corner_right");
	private Asset<Texture2D> _cursorCornerLeft = QuiteEnoughRecipes.Instance.Assets.Request<Texture2D>("Images/cursor_corner_left");
	private Asset<Texture2D> _cursorEdgeH = QuiteEnoughRecipes.Instance.Assets.Request<Texture2D>("Images/cursor_edge_horizontal");
	private Asset<Texture2D> _cursorEdgeV = QuiteEnoughRecipes.Instance.Assets.Request<Texture2D>("Images/cursor_edge_vertical");

	// When not null, we assume this window is being dragged.
	private DragState? _dragState = null;

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

	// Stuff should just be directly appended to this instead of the window itself.
	public UIElement Contents { get; private set; } = new(){
		Width = StyleDimension.Fill,
		Height = new(-BarHeight, 1),
		VAlign = 1,
	};

	public UIWindow()
	{
		BackgroundColor = BarColor * 0.7f;
		BorderColor = BarColor;
		SetPadding(0);
		Contents.SetPadding(ResizeBorderWidth);
		Append(_topBar);
		Append(Contents);
	}

	public override void LeftMouseDown(UIMouseEvent e)
	{
		base.LeftMouseDown(e);

		if (e.Target == this || e.Target == Contents)
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

			var dragState = new DragState{
				OriginalSize = new Vector2(Width.Pixels, Height.Pixels),
				OriginalPos = new Vector2(Left.Pixels, Top.Pixels),
				OriginalMouse = Main.MouseScreen,
			};

			/*
			 * If we're not resizing, then we only want to drag the window if we grabbed it by the
			 * top bar.
			 */
			if (HoveringResize || _topBar.ContainsPoint(Main.MouseScreen))
			{
				_dragState = dragState;
			}
		}
	}

	public override void LeftMouseUp(UIMouseEvent e)
	{
		base.LeftMouseUp(e);
		_dragState = null;
	}

	public override void Update(GameTime t)
	{
		base.Update(t);

		if (IsMouseHovering)
		{
			PlayerInput.LockVanillaMouseScroll("QuiteEnoughRecipes/UIWindow");
			Main.LocalPlayer.mouseInterface = true;
		}
	}

	public CalculatedStyle GetParentDimensions()
	{
		return Parent?.GetInnerDimensions() ?? UserInterface.ActiveInstance.GetDimensions();
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		base.DrawSelf(sb);

		if (_dragState is DragState s)
		{
			var offset = Main.MouseScreen - s.OriginalMouse;

			var parentBounds = GetParentDimensions();
			var parentSize = new Vector2(parentBounds.Width, parentBounds.Height);

			// We're not resizing, so we're dragging the window.
			if (!HoveringResize)
			{
				Left.Pixels = Math.Clamp(s.OriginalPos.X + offset.X, 0, parentSize.X - Width.Pixels);
				Top.Pixels = Math.Clamp(s.OriginalPos.Y + offset.Y, 0, parentSize.Y - Height.Pixels);
			}
			else
			{
				if (_resizeLeft)
				{
					Left.Pixels = Math.Clamp(s.OriginalPos.X + offset.X, 0, s.OriginalPos.X + s.OriginalSize.X - MinWidth.Pixels);
					Width.Pixels = s.OriginalSize.X + s.OriginalPos.X - Left.Pixels;
				}
				else if (_resizeRight)
				{
					Width.Pixels = Math.Clamp(s.OriginalSize.X + offset.X, MinWidth.Pixels, parentSize.X - Left.Pixels);
				}

				if (_resizeTop)
				{
					Top.Pixels = Math.Clamp(s.OriginalPos.Y + offset.Y, 0, s.OriginalPos.Y + s.OriginalSize.Y - MinHeight.Pixels);
					Height.Pixels = s.OriginalSize.Y + s.OriginalPos.Y - Top.Pixels;
				}
				else if (_resizeBottom)
				{
					Height.Pixels = Math.Clamp(s.OriginalSize.Y + offset.Y, MinHeight.Pixels, parentSize.Y - Top.Pixels);
				}
			}

			Recalculate();
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

		// Two sides being resized at once is a corner. One is an edge.
		int numResizeDirs = ((bool[])[_resizeLeft, _resizeRight, _resizeTop, _resizeBottom])
			.Count(b => b);

		if (numResizeDirs == 2)
		{
			if (_resizeLeft && _resizeTop || _resizeBottom && _resizeRight)
			{
				UISystem.CustomCursorTexture = _cursorCornerLeft;
			}
			else
			{
				UISystem.CustomCursorTexture = _cursorCornerRight;
			}

			UISystem.CustomCursorOffset = _cursorCornerLeft.Frame().Size() / 2;
		}
		else if (numResizeDirs == 1)
		{
			if (_resizeLeft || _resizeRight)
			{
				UISystem.CustomCursorTexture = _cursorEdgeH;
				UISystem.CustomCursorOffset = _cursorEdgeH.Frame().Size() / 2;
			}
			else
			{
				UISystem.CustomCursorTexture = _cursorEdgeV;
				UISystem.CustomCursorOffset = _cursorEdgeV.Frame().Size() / 2;
			}
		}
	}
}
