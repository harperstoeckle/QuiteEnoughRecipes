using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Terraria;

namespace QuiteEnoughRecipes;

// Window that appears at the cursor when opened and closes when the cursor leaves.
public class UIPopupWindow : UIWindow
{
	private bool _wasJustOpened = false;

	public void Open()
	{
		UISystem.WindowManager?.Open(this);

		/*
		 * Since `UIWindowManager::Open` defers adding this window to the next frame, we also have
		 * to defer setting the initial position.
		 */
		_wasJustOpened = true;
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		if (_wasJustOpened)
		{
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

			_wasJustOpened = false;
		}
		else if (!ContainsPoint(Main.MouseScreen))
		{
			UISystem.WindowManager?.Close(this);
		}

		/*
		 * Only draw *after* adjusting the position for the first time; otherwise, we draw one frame
		 * of the window in the wrong spot.
		 */
		base.DrawSelf(sb);
	}
}
