using Microsoft.Xna.Framework;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using Terraria;

public class UIWindow : UIPanel
{
	private const float BarHeight = 20;
	private const float ResizeWidth = 10;

	private struct DragState
	{
		public required Vector2 OriginalSize;
		public required Vector2 OriginalPos;
		public required Vector2 OriginalMouse;
	}

	// When not null, we assume this window is being dragged.
	private DragState? _dragState = null;

	private bool _resizeLeft = false;
	private bool _resizeRight = false;
	private bool _resizeTop = false;
	private bool _resizeBottom = false;

	public override void LeftMouseDown(UIMouseEvent e)
	{
		if (e.Target == this)
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

			_dragState = new(){
				OriginalSize = new Vector2(Width.Pixels, Height.Pixels),
				OriginalPos = new Vector2(Left.Pixels, Top.Pixels),
				OriginalMouse = Main.MouseScreen,
			};

			float right = dims.X + dims.Width;
			float bottom = dims.Y + dims.Height;

			_resizeLeft = dims.X <= Main.mouseX && Main.mouseX <= dims.X + ResizeWidth;
			_resizeRight = right - ResizeWidth <= Main.mouseX && Main.mouseX <= right;
			_resizeTop = dims.Y <= Main.mouseY && Main.mouseY <= dims.Y + ResizeWidth;
			_resizeBottom = bottom - ResizeWidth <= Main.mouseY && Main.mouseY <= bottom;
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

			bool dragging = !_resizeLeft && !_resizeRight && !_resizeTop && !_resizeBottom;

			var parentBounds = GetParentDimensions();
			var parentSize = new Vector2(parentBounds.Width, parentBounds.Height);

			if (dragging)
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
	}

	private CalculatedStyle GetParentDimensions()
	{
		return Parent?.GetInnerDimensions() ?? UserInterface.ActiveInstance.GetDimensions();
	}
}
