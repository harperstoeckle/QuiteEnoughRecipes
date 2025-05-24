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
	private static UserInterface? _userInterface;

	public static bool IsFullscreen { get; private set; } = true;

	public static UIQERWindow? Window { get; private set; }
	public static UIWindowManager? WindowManager { get; private set; }

	public static ModKeybind? OpenUIKey { get; private set; }
	public static ModKeybind? HoverSourcesKey { get; private set; }
	public static ModKeybind? HoverUsesKey { get; private set; }
	public static ModKeybind? BackKey { get; private set; }

	public static bool ShouldGoBackInHistory { get; private set; } = false;
	public static bool ShouldGoForwardInHistory { get; private set; } = false;

	// When this is set, this will be drawn instead of the normal cursor.
	public static Asset<Texture2D>? CustomCursorTexture;

	// Offset of the center of the custom cursor texture.
	public static Vector2 CustomCursorOffset = Vector2.Zero;

	public override void Load()
	{
		OpenUIKey = KeybindLoader.RegisterKeybind(Mod, "OpenUI", "OemTilde");
		HoverSourcesKey = KeybindLoader.RegisterKeybind(Mod, "HoverSources", "OemOpenBrackets");
		HoverUsesKey = KeybindLoader.RegisterKeybind(Mod, "HoverUses", "OemCloseBrackets");
		BackKey = KeybindLoader.RegisterKeybind(Mod, "Back", "Back");

		/*
		 * I tried to do this by using an interface layer, but this doesn't quite work properly
		 * when in `IngameFancyUI` because `IngameFancyUI` calls `DrawCursor` itself instead of
		 * using the cursor interface layer. Detouring like this ensures that the custom cursor is
		 * always drawn.
		 */
		On_Main.DrawCursor += DetourDrawCursor;
		On_Main.DrawThickCursor += DetourDrawThickCursor;

		/*
		 * This general method is taken from Magic Storage. Whenever we're hovering anything in the
		 * QER UI, we want to avoid any interaction with the inventory, so we trick the game into
		 * thinking the mouse is off-screen while drawing the inventory.
		 */
		On_Main.DrawInventory += (orig, self) => {
			int oldMouseX = Main.mouseX;
			int oldMouseY = Main.mouseY;

			if (IsHoveringWindow)
			{
				Main.mouseX = -1;
				Main.mouseY = -1;
			}

			orig(self);

			Main.mouseX = oldMouseX;
			Main.mouseY = oldMouseY;
		};

		/*
		 * In vanilla, a lot of this kind of logic (resetting variables) seems to be handled in
		 * interface layers like "Vanilla: Interface Logic 4", but that means that they aren't
		 * always properly called in fancy UI mode. I'm not sure if there's a better place to
		 * handle this stuff.
		 */
		Main.OnPostDraw += ResetPerFrameVariables;
	}

	public override void Unload()
	{
		Main.OnPostDraw -= ResetPerFrameVariables;
	}

	public override void OnWorldLoad()
	{
		IsFullscreen = true;

		/*
		 * We want to reset the UI every time the world loads so it's not carrying over weird state
		 * across worlds. There's also potentially world-specific data that needs to be handled
		 * differently in each world.
		 */
		WindowManager = new();
		Window = new();
		_userInterface = new();

		WindowManager.Open(Window);

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
			_userInterface?.Update(t);
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
						_userInterface?.Draw(Main.spriteBatch, new GameTime());
					}
					return true;
				},
				InterfaceScaleType.UI));
	}

	// This needs the parameter to be added to the `Main::OnPostDraw` event.
	public static void ResetPerFrameVariables(GameTime t)
	{
		CustomCursorTexture = null;
		CustomCursorOffset = Vector2.Zero;
	}

	/*
	 * `Open` and `Close` are the preferred ways to open and close the browser, since they handle
	 * things like auto-focusing the search bar.
	 */
	public static void Open()
	{
		if (IsFullscreen)
		{
			IngameFancyUI.OpenUIState(WindowManager);
		}
		else
		{
			Main.playerInventory = true;
			_userInterface?.SetState(WindowManager);
		}

		Window?.Open();
		Window?.Recalculate();
	}

	public static void Close()
	{
		_userInterface?.SetState(null);
		IngameFancyUI.Close();

		Window?.Close();
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
		IsFullscreen = !IsFullscreen;
		Open();
	}

	public static void ShowSources(IIngredient i) => Window?.ShowSources(i);
	public static void ShowUses(IIngredient i) => Window?.ShowUses(i);

	public static bool IsOpen()
	{
		return WindowManager != null && (Main.InGameUI.CurrentState == WindowManager || _userInterface?.CurrentState == WindowManager && Main.playerInventory);
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

	private static void DetourDrawCursor(On_Main.orig_DrawCursor orig, Vector2 bonus, bool smart)
	{
		if (CustomCursorTexture is not null)
		{
			var color = Main.cursorColor;
			if (Main.LocalPlayer.hasRainbowCursor)
			{
				color = Main.hslToRgb(Main.GlobalTimeWrappedHourly * 0.25f % 1f, 1f, 0.5f, 255);
			}

			Main.spriteBatch.Draw(CustomCursorTexture.Value, Main.MouseScreen, null, color, 0,
					CustomCursorOffset, Main.cursorScale, 0, 0);
		}
		else
		{
			orig(bonus, smart);
		}
	}

	private static Vector2 DetourDrawThickCursor(On_Main.orig_DrawThickCursor orig, bool smart)
	{
		// Copied from `DrawThickCursor`
		if (CustomCursorTexture is not null)
		{
			var offsets = (Vector2[])[new Vector2(0, 1), new Vector2(1, 0), new Vector2(0, -1),
					new Vector2(-1, 0)];
			foreach (var offset in offsets)
			{
				float scale = Main.cursorScale * 1.1f;
				Main.spriteBatch.Draw(CustomCursorTexture.Value, Main.MouseScreen + offset, null,
						Main.MouseBorderColor, 0, CustomCursorOffset, scale, 0, 0);
			}

			return Vector2.Zero;
		}

		return orig(smart);
	}

	private static bool IsHoveringWindow => WindowManager is not null && WindowManager.IsHoveringWindow;
}
