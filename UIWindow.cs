using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using Terraria;

public class UIWindow : UIPanel
{
	private const float BarHeight = 20;
	private const float ResizeWidth = 10;

	// When not null, we assume this window is being dragged.
	private Vector2? _dragOffset = null;

	public UIWindow()
	{
	}

	public override void LeftMouseDown(UIMouseEvent e)
	{
		if (e.Target == this)
		{
			_dragOffset =  new Vector2(Left.Pixels, Top.Pixels) - Main.MouseScreen;
		}
	}

	public override void LeftMouseUp(UIMouseEvent e)
	{
		_dragOffset = null;
	}

	public override void Update(GameTime t)
	{
		base.Update(t);

		if (_dragOffset is Vector2 offset)
		{
			var newPos = Main.MouseScreen + offset;

			var parentBounds = Parent?.GetInnerDimensions() ?? new();
			var parentSize = new Vector2(parentBounds.Width, parentBounds.Height);
			var size = new Vector2(Width.Pixels, Height.Pixels);

			// Keep the windows inside the screen, and adjust the mouse offset to match.
			newPos = Vector2.Clamp(newPos, Vector2.Zero, parentSize - size);

			Left.Pixels = newPos.X;
			Top.Pixels = newPos.Y;

			Recalculate();
		}
	}
}
