using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader.UI;
using Terraria.UI.Chat;
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
		private int _npcID = 0;
		private bool _isHovering => Parent?.IsMouseHovering ?? false;

		public BestiaryEntry Entry { get; private set; }
		public required int NPCID
		{
			get => _npcID;

			[MemberNotNull(nameof(Entry))]
			set
			{
				_npcID = value;
				Entry = Main.BestiaryDB.FindEntryByNPCID(_npcID);
			}
		}

		public UINPCIcon()
		{
			OverflowHidden = true;
			IgnoresMouseInteraction = true;
			Width.Percent = 1;
			Height.Percent = 1;
		}

		public override void Update(GameTime t)
		{
			if (Entry.Icon == null) { return; }

			var rect = GetDimensions().ToRectangle();
			var collectionInfo = new BestiaryUICollectionInfo(){
				OwnerEntry = Entry,
				UnlockState = BestiaryEntryUnlockState.CanShowPortraitOnly_1
			};

			if (QuiteEnoughRecipes.LoadNPCAsync(NPCID).IsLoaded)
			{
				Entry.Icon.Update(collectionInfo, rect,
				new EntryIconDrawSettings()
				{
					iconbox = rect,
					IsHovered = _isHovering,
					IsPortrait = false
				});
			}	
		}

		protected override void DrawSelf(SpriteBatch sb)
		{
			if (Entry.Icon == null) { return; }

			var collectionInfo = new BestiaryUICollectionInfo(){
				OwnerEntry = Entry,
				UnlockState = BestiaryEntryUnlockState.CanShowPortraitOnly_1
			};

			if (QuiteEnoughRecipes.LoadNPCAsync(NPCID).IsLoaded)
			{
				Entry.Icon.Draw(collectionInfo, sb,
				new EntryIconDrawSettings()
				{
					iconbox = GetDimensions().ToRectangle(),
					IsHovered = _isHovering,
					IsPortrait = false
				});
			}
		}
	}

	public static int GridSideLength => 72;
	public static int GridPadding => 5;

	private UINPCIcon _icon;
	private string _hoverText = "";

	public IIngredient Ingredient => new NPCIngredient(_icon.NPCID);

	public UINPCPanel(int npcID)
	{
		_icon = new UINPCIcon{ NPCID = npcID};
		UpdateHoverText();

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
		_icon.NPCID = i.ID;
		UpdateHoverText();
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		base.DrawSelf(sb);

		if (IsMouseHovering)
		{
			UICommon.TooltipMouseText(_hoverText);
		}
	}

	private void UpdateHoverText()
	{
		ContentSamples.NpcsByNetId.TryGetValue(_icon.NPCID, out var npc);

		/*
		 * We color NPC names by rarity, similarly to items. Bosses use the same color as quest
		 * fish.
		 */
		int rarity = _icon.Entry.Info.Any(i => i is BossBestiaryInfoElement)
			? ItemRarityID.Quest
			: (npc?.rarity ?? 0);

		var rarityColor = ItemRarity.GetColor(rarity);
		var mod = npc?.ModNPC?.Mod;
		var modTag = mod == null ? "" : QuiteEnoughRecipes.GetModTagText(mod);

		_hoverText = $"[c/{rarityColor.Hex3()}:{Lang.GetNPCNameValue(_icon.NPCID)}]{modTag}";
		var flavorText = Ingredient.GetTooltipLines().FirstOrDefault();

		if (flavorText != null)
		{
			// Match width to the width of the name for long names.
			float width = ChatManager.GetStringSize(FontAssets.MouseText.Value, _hoverText,
				Vector2.One).X;
			width = MathF.Max(300, width);

			var wrappedFlavorText = FontAssets.MouseText.Value.CreateWrappedText(flavorText, width);
			_hoverText += $"\n{wrappedFlavorText}";
		}
	}
}
