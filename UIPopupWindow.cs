using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Terraria.Localization;
using Terraria;

namespace QuiteEnoughRecipes;

// Window that appears at the cursor when opened and closes when the cursor leaves.
public class UIPopupWindow : UIWindow
{
	// When true, this window will not disappear when the cursor leaves.
	private bool _isPinned = false;
	private UIQERButton _pinButton = new(QERAssets.ButtonPin, 2);

	private LocalizedText PinButtonText =>
		_isPinned
		? Language.GetText("Mods.QuiteEnoughRecipes.UI.PinDownHover")
		: Language.GetText("Mods.QuiteEnoughRecipes.UI.PinUpHover");

	public UIPopupWindow()
	{
		_pinButton.OnLeftClick += (elem, evt) => {
			_isPinned = !_isPinned;
			UpdatePinButton();
		};
		UpdatePinButton();
		AddElementToBar(_pinButton);
	}

	public override void OnOpen()
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
	}

	public override void OnClose()
	{
		_isPinned = false;
		UpdatePinButton();
	}

	public override void Update(GameTime t)
	{
		if (!_isPinned && !IsDraggingOrResizing && !ContainsPoint(Main.MouseScreen))
		{
			this.Close();
		}
	}

	private void UpdatePinButton()
	{
		_pinButton.Frame = _isPinned ? 1 : 0;
		_pinButton.HoverText = PinButtonText;
	}
}
