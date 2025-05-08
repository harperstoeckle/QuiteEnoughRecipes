using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

public class UISystem : ModSystem
{
	private static UserInterface _userInterface;
	private static bool _isFullscreen = true;

	public static UIQERState? UI { get; private set; }

	public static ModKeybind? OpenUIKey { get; private set; }
	public static ModKeybind? HoverSourcesKey { get; private set; }
	public static ModKeybind? HoverUsesKey { get; private set; }
	public static ModKeybind? BackKey { get; private set; }
	public static ModKeybind? ToggleFullscreenKey { get; private set; }


	public static bool ShouldGoBackInHistory { get; private set; } = false;
	public static bool ShouldGoForwardInHistory { get; private set; } = false;

	/*
	 * When set to a texture, this will be rendered at the bottom right of the mouse cursor. It's
	 * reset to null every frame, so it must be constantly set whenever it should be different.
	 */
	public static Asset<Texture2D>? CursorOverlay;

	public override void Load()
	{
		OpenUIKey = KeybindLoader.RegisterKeybind(Mod, "OpenUI", "OemTilde");
		HoverSourcesKey = KeybindLoader.RegisterKeybind(Mod, "HoverSources", "OemOpenBrackets");
		HoverUsesKey = KeybindLoader.RegisterKeybind(Mod, "HoverUses", "OemCloseBrackets");
		BackKey = KeybindLoader.RegisterKeybind(Mod, "Back", "Back");
		ToggleFullscreenKey = KeybindLoader.RegisterKeybind(Mod, "ToggleFullscreen", "OemBackslash");
	}

	public override void OnWorldLoad()
	{
		_userInterface = new();

		/*
		 * We want to reset the UI every time the world loads so it's not carrying over weird state
		 * across worlds. There's also potentially world-specific data that needs to be handled
		 * differently in each world.
		 */
		UI = new();

		_isFullscreen = true;

		// Loading items beforehand ensures that they *aren't* being loaded while scrolling.
		if (QERConfig.Instance.ShouldPreloadItems)
		{
			/*
			 * TODO: Since modded items are already loaded, should I only do this with vanilla
			 * items?
			 */
			for (int i = 0; i < ItemLoader.ItemCount; ++i)
			{
				QuiteEnoughRecipes.LoadItemAsync(i);
			}
		}
	}

	public override void UpdateUI(GameTime t)
	{
		/*
		 * This has to be handled here to work even when autopaused. This is (I think) the same set
		 * of conditions under which `ModPlayer::ProcessTriggers` is called.
		 */
		if (Main.hasFocus && !Main.drawingPlayerChat && !Main.editSign && !Main.editChest
				&& !Main.blockInput)
		{
			HandleInput();
		}

		if (Main.playerInventory)
		{
			_userInterface.Update(t);
		}
	}

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int mouseTextLayer = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
		if (mouseTextLayer == -1) { return; }

		layers.Insert(
			mouseTextLayer,
			new LegacyGameInterfaceLayer(
				"QuiteEnoughRecipes: Interface",
				() => {
					if (Main.playerInventory)
					{
						_userInterface.Draw(Main.spriteBatch, new GameTime());
					}
					return true;
				},
				InterfaceScaleType.UI));

		int cursorLayer = layers.FindIndex(l => l.Name == "Vanilla: Cursor");
		if (cursorLayer == -1) { return; }

		layers.Insert(
			cursorLayer + 1,
			new LegacyGameInterfaceLayer(
				"QuiteEnoughRecipes: Mouse Overlay",
				() => {
					if (CursorOverlay is not null)
					{
						var overlayOffset = Main.cursorScale * new Vector2(15, 15);

						Main.spriteBatch.Draw(CursorOverlay.Value,
								Main.MouseScreen + overlayOffset, null, Color.White, 0,
								Vector2.Zero, new Vector2(Main.cursorScale), 0, 0);

						CursorOverlay = null;
					}

					return true;
				},
				InterfaceScaleType.UI));
	}

	/*
	 * `Open` and `Close` are the preferred ways to open and close the browser, since they handle
	 * things like auto-focusing the search bar.
	 */
	public static void Open()
	{
		if (_isFullscreen)
		{
			IngameFancyUI.OpenUIState(UI);
		}
		else
		{
			Main.playerInventory = true;
			_userInterface.SetState(UI);
		}

		UI?.Open();
		UI?.Recalculate();
	}

	public static void Close()
	{
		_userInterface.SetState(null);
		IngameFancyUI.Close();

		UI?.Close();
	}

	public static void ToggleOpen()
	{
		if (IsOpen())
		{
			Close();
		}
		else
		{
			Open();
		}
	}

	public static void ToggleFullscreen()
	{
		Close();
		_isFullscreen = !_isFullscreen;
		Open();
	}

	public static void ShowSources(IIngredient i) => UI?.ShowSources(i);
	public static void ShowUses(IIngredient i) => UI?.ShowUses(i);

	public static bool IsOpen()
	{
		return UI != null && (Main.InGameUI.CurrentState == UI || _userInterface.CurrentState == UI && Main.playerInventory);
	}

	private void HandleInput()
	{
		ShouldGoBackInHistory = false;
		ShouldGoForwardInHistory = false;

		if (BackKey?.JustPressed ?? false)
		{
			ShouldGoForwardInHistory = Main.keyState.PressingShift();
			ShouldGoBackInHistory = !ShouldGoForwardInHistory;
		}

		if (OpenUIKey?.JustPressed ?? false)
		{
			ToggleOpen();
		}

		if ((HoverSourcesKey?.JustPressed ?? false) && Main.HoverItem != null && !Main.HoverItem.IsAir)
		{
			ShowSources(new ItemIngredient(Main.HoverItem));
			Open();
		}

		if ((HoverUsesKey?.JustPressed ?? false) && Main.HoverItem != null && !Main.HoverItem.IsAir)
		{
			ShowUses(new ItemIngredient(Main.HoverItem));
			Open();
		}
	}
}
