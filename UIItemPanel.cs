using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

/*
 * Displays an item slot that can be hovered to show a tooltip for an item. By default, it does not
 * have any other special behavior when interacted with; this can be achieved by subscribing to the
 * events from `UIElement`.
 */
public class UIItemPanel : UIElement
{
	private float _scale;

	// The item to show. When this is set to `null`, nothing at all will be drawn.
	public Item? DisplayedItem;

	// The icon will be scaled to fit in a square with side length `width`.
	public UIItemPanel(Item? displayedItem = null, float width = 50)
	{
		DisplayedItem = displayedItem;
		Width.Pixels = width;
		Height.Pixels = width;
		_scale = width / 50;
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		if (DisplayedItem == null) { return; }

		float prevScale = Main.inventoryScale;
		Main.inventoryScale = _scale;

		// TODO: Change the context to something other than -1.
		ItemSlot.Draw(sb, ref DisplayedItem, -1, GetInnerDimensions().Position());

		Main.inventoryScale = prevScale;

		if (IsMouseHovering)
		{
			Main.instance.MouseText("");
			Main.HoverItem = DisplayedItem.Clone();
		}
	}
}
