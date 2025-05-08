using Microsoft.Xna.Framework;
using System.Linq;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

public class UIWindow : UIPanel
{
	private const float BarHeight = 30;
	private const float ResizeWidth = 5;

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
	};

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
		BackgroundColor = Color.Transparent;
		SetPadding(0);
		Contents.SetPadding(ResizeWidth);
		Append(_topBar);
		Append(Contents);
	}

	public override void LeftMouseDown(UIMouseEvent e)
	{
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

			_resizeLeft = dims.X <= Main.mouseX && Main.mouseX <= dims.X + ResizeWidth;
			_resizeRight = right - ResizeWidth <= Main.mouseX && Main.mouseX <= right;
			_resizeTop = dims.Y <= Main.mouseY && Main.mouseY <= dims.Y + ResizeWidth;
			_resizeBottom = bottom - ResizeWidth <= Main.mouseY && Main.mouseY <= bottom;

		}

		// Two sides being resized at once is a corner. One is an edge.
		int numResizeDirs = ((bool[])[_resizeLeft, _resizeRight, _resizeTop, _resizeBottom])
			.Count(b => b);

		if (numResizeDirs == 2)
		{
			UISystem.CursorOverlay = TextureAssets.Camera[2];
		}
		else if (numResizeDirs == 1)
		{
			UISystem.CursorOverlay = TextureAssets.Camera[3];
		}
	}

	private CalculatedStyle GetParentDimensions()
	{
		return Parent?.GetInnerDimensions() ?? UserInterface.ActiveInstance.GetDimensions();
	}
}
