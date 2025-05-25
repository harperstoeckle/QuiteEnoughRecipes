using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

public class UIQERWindow : UIWindow
{
	// Never used as actual UI elements. Just used to store dimensions.
	private UIElement _windowedStyleRef = new(){
		MinWidth = new(200, 0),
		MinHeight = new(200, 0),
		Width = new(-500, 1),
		Height = new(-400, 1),
		Left = new(300, 0),
		Top = new(320, 0),
	};

	private UIElement _fullscreenStyleRef = new(){
		Width = new(0, 0.95f),
		Height = new(0, 0.8f),
		HAlign = 0.5f,
		VAlign = 0.5f,
	};

	private LocalizedText FullscreenButtonText =>
		UISystem.IsFullscreen
		? Language.GetText("Mods.QuiteEnoughRecipes.UI.FullscreenHover")
		: Language.GetText("Mods.QuiteEnoughRecipes.UI.WindowedHover");


	public UIQERWindow()
	{
		CopyStyle(_fullscreenStyleRef);
		CanDragOrResize = false;

		// This window should always be behind other windows.
		WindowState.ZOrder = -1;

		var fullscreenButton = new UIQERButton(QERAssets.ButtonFullscreen, 2);
		fullscreenButton.Frame = UISystem.IsFullscreen ? 1 : 0;
		fullscreenButton.HoverText = FullscreenButtonText;
		fullscreenButton.OnLeftClick += (elem, evt) => {
			UISystem.ToggleFullscreen();
			fullscreenButton.Frame = UISystem.IsFullscreen ? 1 : 0;
			fullscreenButton.HoverText = FullscreenButtonText;

			if (UISystem.IsFullscreen)
			{
				_windowedStyleRef.CopyStyle(this);
				CopyStyle(_fullscreenStyleRef);
			}
			else
			{
				CopyStyle(_windowedStyleRef);
			}

			CanDragOrResize = !UISystem.IsFullscreen;
		};

		AddElementToBar(fullscreenButton);
		Contents.Append(new UITilingWindowContainer{ Width = new(0, 1), Height = new(0, 1) });
	}

	/*
	 * We want to close the whole interface instead of just the window. Since this is the main
	 * window that we access stuff through, closing it without closing the interface would cause
	 * problems.
	 */
	protected override void PressCloseButton() => UISystem.Close();

}
