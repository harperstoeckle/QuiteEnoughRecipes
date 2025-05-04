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
			_dragOffset =  GetOuterDimensions().Position() - Main.MouseScreen;
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
			var dims = GetOuterDimensions();
			var size = new Vector2(dims.Width, dims.Height);
			var screenSize = Main.ScreenSize.ToVector2();

			var newPos = Main.MouseScreen + offset;

			// Keep the windows inside the screen, and adjust the mouse offset to match.
			newPos = Vector2.Clamp(newPos, Vector2.Zero, screenSize - size);
			_dragOffset = newPos - Main.MouseScreen;

			Left.Pixels = newPos.X;
			Top.Pixels = newPos.Y;

			Recalculate();
		}
	}
}
