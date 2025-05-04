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

	public UIWindow()
	{
	}

	public override void LeftMouseDown(UIMouseEvent e)
	{
		if (e.Target == this)
		{
			_dragState = new(){
				OriginalSize = new Vector2(Width.Pixels, Height.Pixels),
				OriginalPos = new Vector2(Left.Pixels, Top.Pixels),
				OriginalMouse = Main.MouseScreen,
			};

			var dims = GetOuterDimensions();

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

			bool inMiddle = !_resizeLeft && !_resizeRight && !_resizeTop && !_resizeBottom;

			float leftOffset = _resizeLeft || inMiddle ? offset.X : 0;
			float widthOffset = (_resizeRight || inMiddle ? offset.X : 0) - leftOffset;
			float topOffset = _resizeTop || inMiddle ? offset.Y : 0;
			float heightOffset = (_resizeBottom || inMiddle ? offset.Y : 0) - topOffset;

			var parentBounds = Parent?.GetInnerDimensions() ?? new();
			var parentSize = new Vector2(parentBounds.Width, parentBounds.Height);
			var size = new Vector2(Width.Pixels, Height.Pixels);

			Left.Pixels = s.OriginalPos.X + leftOffset;
			Width.Pixels = s.OriginalSize.X + widthOffset;
			Top.Pixels = s.OriginalPos.Y + topOffset;
			Height.Pixels = s.OriginalSize.Y + heightOffset;

			Recalculate();
		}
	}
}
