using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
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

// Displays an item group by cycling through items in that group.
public class UIRecipeGroupPanel : UIItemPanel
{
	// Amount of time to wait before the displayed item is cycled.
	private static readonly TimeSpan TimePerCycle = new(0, 0, 0, 1);

	private TimeSpan _timeSinceLastCycle = new(0);
	private List<int> _itemsInGroup;
	private int _curItemIndex = 0;

	public UIRecipeGroupPanel(RecipeGroup displayedGroup, int stack = 1, float width = 50) :
		base(null, width)
	{
		DisplayedItem = new Item(displayedGroup.IconicItemId, stack);
		_itemsInGroup = new(displayedGroup.ValidItems);
	}

	public override void Update(GameTime t)
	{
		if (_itemsInGroup.Count == 0) { return; }

		_timeSinceLastCycle += t.ElapsedGameTime;

		while (_timeSinceLastCycle >= TimePerCycle)
		{
			_timeSinceLastCycle -= TimePerCycle;
			_curItemIndex = (_curItemIndex + 1) % _itemsInGroup.Count;
		}

		// We only change the type instead of the
		DisplayedItem.type = _itemsInGroup[_curItemIndex];
	}
}
