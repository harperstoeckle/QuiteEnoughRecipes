using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

/*
 * Displays an NPC. Similar to a bestiary button, but is never locked. Shows the name of the NPC
 * when hovered.
 */
public class UINPCPanel : UIElement, IIngredientElement, IScrollableGridElement<NPCIngredient>
{
	// This needs to be a child of the panel to handle overflow properly.
	private class UINPCIcon : UIElement
	{
		private bool _isHovering => Parent?.IsMouseHovering ?? false;

		public BestiaryEntry _entry;

		public UINPCIcon(int npcID)
		{
			_entry = Main.BestiaryDB.FindEntryByNPCID(npcID);

			OverflowHidden = true;
			IgnoresMouseInteraction = true;
			Width.Percent = 1;
			Height.Percent = 1;
		}

		public override void Update(GameTime t)
		{
			var rect = GetDimensions().ToRectangle();
			var collectionInfo = new BestiaryUICollectionInfo(){
				OwnerEntry = _entry,
				UnlockState = BestiaryEntryUnlockState.CanShowPortraitOnly_1
			};
			_entry.Icon?.Update(collectionInfo, rect,
				new EntryIconDrawSettings(){
					iconbox = rect,
					IsHovered = _isHovering,
					IsPortrait = false
				});
		}

		protected override void DrawSelf(SpriteBatch sb)
		{
			var collectionInfo = new BestiaryUICollectionInfo(){
				OwnerEntry = _entry,
				UnlockState = BestiaryEntryUnlockState.CanShowPortraitOnly_1
			};

			_entry.Icon?.Draw(collectionInfo, sb,
				new EntryIconDrawSettings(){
					iconbox = GetDimensions().ToRectangle(),
					IsHovered = _isHovering,
					IsPortrait = false
				});
		}
	}

	private UINPCIcon _icon;
	private int _npcID;

	public IIngredient Ingredient => new NPCIngredient(_npcID);

	public UINPCPanel(int npcID)
	{
		_icon = new(npcID);
		_npcID = npcID;

		Width.Pixels = Height.Pixels = 72;
		OverflowHidden = true;

		var slotBack = new UIImage(Main.Assets.Request<Texture2D>("Images/UI/Bestiary/Slot_Back")){
			Width = new StyleDimension(0, 1),
			Height = new StyleDimension(0, 1),
			HAlign = 0.5f,
			VAlign = 0.5f,
			ScaleToFit = true,
			IgnoresMouseInteraction = true
		};
		var slotFront = new UIImage(Main.Assets.Request<Texture2D>("Images/UI/Bestiary/Slot_Front")){
			Width = new StyleDimension(0, 1),
			Height = new StyleDimension(0, 1),
			HAlign = 0.5f,
			VAlign = 0.5f,
			ScaleToFit = true,
			IgnoresMouseInteraction = true
		};

		Append(slotBack);
		Append(_icon);
		Append(slotFront);
	}

	public UINPCPanel() : this(0) {}

	public void SetDisplayedValue(NPCIngredient i)
	{
		_npcID = i.ID;
		_icon._entry = Main.BestiaryDB.FindEntryByNPCID(_npcID);
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		base.DrawSelf(sb);

		var collectionInfo = new BestiaryUICollectionInfo(){
			OwnerEntry = _icon._entry,
			UnlockState = BestiaryEntryUnlockState.CanShowPortraitOnly_1
		};

		if (IsMouseHovering)
		{
			Main.instance.MouseText(
				_icon._entry.Icon?.GetHoverText(collectionInfo) ?? Lang.GetNPCNameValue(_npcID));
		}
	}
}
