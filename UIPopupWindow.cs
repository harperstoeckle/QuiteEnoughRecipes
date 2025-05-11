using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Terraria;

namespace QuiteEnoughRecipes;

// Window that appears at the cursor when opened and closes when the cursor leaves.
public class UIPopupWindow : UIWindow
{
	public void Open()
	{
		UISystem.WindowManager?.Open(this);

		var dims = GetParentDimensions();

		var mousePos = Main.MouseScreen - dims.Position();
		var popupSize = GetOuterDimensions().ToRectangle().Size();

		float xOffset = 15;
		float yOffset = 50;
		var pos = mousePos - new Vector2(popupSize.X - xOffset, yOffset);

		if (pos.X < 0)
		{
			pos.X = mousePos.X - xOffset;
		}
		if (pos.Y + popupSize.Y > dims.Height)
		{
			pos.Y = dims.Height - popupSize.Y;
		}

		Left.Pixels = pos.X;
		Top.Pixels = pos.Y;

		Recalculate();
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		base.DrawSelf(sb);
		if (!ContainsPoint(Main.MouseScreen)) { UISystem.WindowManager?.Close(this); }
	}
}
