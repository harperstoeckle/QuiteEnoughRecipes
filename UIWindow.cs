using Microsoft.Xna.Framework;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using Terraria;

public class UIWindow : UIPanel
{
	private const float BarHeight = 30;
	private const float ResizeWidth = 5;

	private struct DragState
	{
		public required Vector2 OriginalSize;
		public required Vector2 OriginalPos;
		public required Vector2 OriginalMouse;

		public bool ResizeLeft = false;
		public bool ResizeRight = false;
		public bool ResizeTop = false;
		public bool ResizeBottom = false;

		public DragState() {}

		public bool IsResizing => ResizeLeft || ResizeRight || ResizeTop || ResizeBottom;
	}

	private UIPanel _topBar = new(){
		Width = StyleDimension.Fill,
		Height = new(BarHeight, 0),
		IgnoresMouseInteraction = true,
	};

	// When not null, we assume this window is being dragged.
	private DragState? _dragState = null;

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

			float right = dims.X + dims.Width;
			float bottom = dims.Y + dims.Height;

			var dragState = new DragState{
				OriginalSize = new Vector2(Width.Pixels, Height.Pixels),
				OriginalPos = new Vector2(Left.Pixels, Top.Pixels),
				OriginalMouse = Main.MouseScreen,
			};

			dragState.ResizeLeft = dims.X <= Main.mouseX && Main.mouseX <= dims.X + ResizeWidth;
			dragState.ResizeRight = right - ResizeWidth <= Main.mouseX && Main.mouseX <= right;
			dragState.ResizeTop = dims.Y <= Main.mouseY && Main.mouseY <= dims.Y + ResizeWidth;
			dragState.ResizeBottom = bottom - ResizeWidth <= Main.mouseY && Main.mouseY <= bottom;

			/*
			 * If we're not resizing, then we only want to drag the window if we grabbed it by the
			 * top bar.
			 */
			if (dragState.IsResizing || _topBar.ContainsPoint(Main.MouseScreen))
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

		if (_dragState is DragState s)
		{
			var offset = Main.MouseScreen - s.OriginalMouse;

			var parentBounds = GetParentDimensions();
			var parentSize = new Vector2(parentBounds.Width, parentBounds.Height);

			// We're not resizing, so we're dragging the window.
			if (!s.IsResizing)
			{
				Left.Pixels = Math.Clamp(s.OriginalPos.X + offset.X, 0, parentSize.X - Width.Pixels);
				Top.Pixels = Math.Clamp(s.OriginalPos.Y + offset.Y, 0, parentSize.Y - Height.Pixels);
			}
			else
			{
				if (s.ResizeLeft)
				{
					Left.Pixels = Math.Clamp(s.OriginalPos.X + offset.X, 0, s.OriginalPos.X + s.OriginalSize.X - MinWidth.Pixels);
					Width.Pixels = s.OriginalSize.X + s.OriginalPos.X - Left.Pixels;
				}
				else if (s.ResizeRight)
				{
					Width.Pixels = Math.Clamp(s.OriginalSize.X + offset.X, MinWidth.Pixels, parentSize.X - Left.Pixels);
				}

				if (s.ResizeTop)
				{
					Top.Pixels = Math.Clamp(s.OriginalPos.Y + offset.Y, 0, s.OriginalPos.Y + s.OriginalSize.Y - MinHeight.Pixels);
					Height.Pixels = s.OriginalSize.Y + s.OriginalPos.Y - Top.Pixels;
				}
				else if (s.ResizeBottom)
				{
					Height.Pixels = Math.Clamp(s.OriginalSize.Y + offset.Y, MinHeight.Pixels, parentSize.Y - Top.Pixels);
				}
			}

			Recalculate();
		}
	}

	private CalculatedStyle GetParentDimensions()
	{
		return Parent?.GetInnerDimensions() ?? UserInterface.ActiveInstance.GetDimensions();
	}
}
