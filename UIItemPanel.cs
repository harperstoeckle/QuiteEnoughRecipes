using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Chat;
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

		// Extremely lazy way to handle text bounds.
		SetPadding(3 * _scale);
	}

	/*
	 * This is a stripped-down version of the vanilla drawing code. It doesn't have to do any of
	 * the "fancy" stuff that vanilla has to do to handle item slots in specific contexts.
	 */
	protected override void DrawSelf(SpriteBatch sb)
	{
		if (DisplayedItem == null) { return; }

		var pos = GetDimensions().Position();

		var inventoryBack = TextureAssets.InventoryBack.Value;
		sb.Draw(inventoryBack, pos, null, Color.White, 0, Vector2.Zero, _scale, 0, 0);

		QuiteEnoughRecipes.DrawItemIcon(DisplayedItem, -1, sb, pos + inventoryBack.Size() * _scale / 2,
			_scale, 32, Color.White);

		// Draw trapped chest indicator.
		if (ItemID.Sets.TrapSigned[DisplayedItem.type])
		{
			sb.Draw(TextureAssets.Wire.Value, pos + new Vector2(40) * _scale,
				new Rectangle(4, 58, 8, 8), Color.White, 0f, new Vector2(4), 1, 0, 0);
		}

		// Draw unsafe wall indicator.
		if (ItemID.Sets.DrawUnsafeIndicator[DisplayedItem.type])
		{
			var indicator = TextureAssets.Extra[ExtrasID.UnsafeIndicator].Value;
			var frame = indicator.Frame();
			sb.Draw(indicator, pos + new Vector2(36) * _scale, frame, Color.White, 0,
				frame.Size() / 2, 1, 0, 0);
		}

		// TODO: Add rubblemaker indicator?

		DrawOverlayText(sb);

		if (IsMouseHovering)
		{
			Main.instance.MouseText("");
			Main.HoverItem = DisplayedItem.Clone();
		}
	}

	/*
	 * Special item panels can override this to display their own tooltips when they are being
	 * hovered. This is only called for item panels in the QER browser.
	 */
	public virtual void ModifyTooltips(Mod mod, List<TooltipLine> tooltips)
	{
		if (DisplayedItem == null || tooltips.Count <= 0) { return; }

		if (DisplayedItem.ModItem != null)
		{
			tooltips[0].Text += $" [{DisplayedItem.ModItem.Mod.DisplayNameClean}]";
		}
	}

	/*
	 * Draw the text on top of the item slot. This is usually just the stack size, but it can be
	 * overridden.
	 */
	protected virtual void DrawOverlayText(SpriteBatch sb)
	{
		if (DisplayedItem != null && DisplayedItem.stack > 1)
		{
			DrawText(sb, DisplayedItem.stack.ToString(), new Vector2(10, 26));
		}
	}

	/*
	 * Draw text similar to the stack size text. `offset` is the unscaled offset of the text from
	 * the top left corner of the slot. If the text would not fit, it will be shifted left or
	 * shrunk as needed to fit.
	 */
	protected void DrawText(SpriteBatch sb, string s, Vector2 offset)
	{
		// Make this relative to the inner dimensions instead of the main dimensions.
		offset.X -= PaddingLeft;
		offset.Y -= PaddingTop;

		var dims = GetInnerDimensions();
		var scaledOffset = offset * _scale;
		var textScale = _scale;
		var defaultTextSize = ChatManager.GetStringSize(FontAssets.ItemStack.Value, s,
			new Vector2(textScale));

		/*
		 * We assume that text less than a pixel wide just can't be drawn. This also ensures that
		 * we don't have any weird division by zero issues.
		 */
		if (defaultTextSize.X < 1) { return; }

		if (defaultTextSize.X > dims.Width)
		{
			textScale *= dims.Width / defaultTextSize.X;
			scaledOffset.X = 0;
		}
		else if (defaultTextSize.X > dims.ToRectangle().Width - scaledOffset.X)
		{
			scaledOffset.X = dims.ToRectangle().Width - defaultTextSize.X;
		}

		ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.ItemStack.Value, s,
			dims.Position() + scaledOffset, Color.White, 0, Vector2.Zero, new Vector2(textScale),
			-1, textScale);
	}
}

// Displays an item group by cycling through items in that group.
public class UIRecipeGroupPanel : UIItemPanel
{
	// Amount of time to wait before the displayed item is cycled.
	private static readonly TimeSpan TimePerCycle = new(0, 0, 0, 1);

	private TimeSpan _timeSinceLastCycle = new(0);
	private RecipeGroup _displayedGroup;
	private List<int> _itemsInGroup;
	private int _curItemIndex = 0;

	public UIRecipeGroupPanel(RecipeGroup displayedGroup, int stack = 1, float width = 50) :
		base(null, width)
	{
		_displayedGroup = displayedGroup;
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

		int stack = DisplayedItem.stack;
		DisplayedItem.SetDefaults(_itemsInGroup[_curItemIndex]);
		DisplayedItem.stack = stack;
	}

	public override void ModifyTooltips(Mod mod, List<TooltipLine> tooltips)
	{
		base.ModifyTooltips(mod, tooltips);

		if (tooltips.Count <= 0) { return; }
		tooltips.Insert(0, new(mod, "QER: recipe group", _displayedGroup.GetText()){
			OverrideColor = Main.OurFavoriteColor
		});
	}
}
