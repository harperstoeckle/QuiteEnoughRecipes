using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace QuiteEnoughRecipes;

public class QERAssets : ModSystem
{
	// These should never be null when they're used, since they're loaded in `Load`.
#nullable disable
	public static Asset<Texture2D> CursorCornerRight;
	public static Asset<Texture2D> CursorCornerLeft;
	public static Asset<Texture2D> CursorEdgeHorizontal;
	public static Asset<Texture2D> CursorEdgeVertical;
	public static Asset<Texture2D> CursorIBeam;

	public static Asset<Texture2D> ButtonClose;
	public static Asset<Texture2D> ButtonHelp;
	public static Asset<Texture2D> ButtonPin;
	public static Asset<Texture2D> ButtonFullscreen;

	public static Asset<Texture2D> PanelSearchBar;
#nullable enable

	public override void Load()
	{
		CursorCornerRight = LoadTexture("Images/cursor_corner_right");
		CursorCornerLeft = LoadTexture("Images/cursor_corner_left");
		CursorEdgeHorizontal = LoadTexture("Images/cursor_edge_horizontal");
		CursorEdgeVertical = LoadTexture("Images/cursor_edge_vertical");
		CursorIBeam = LoadTexture("Images/cursor_i_beam");

		ButtonClose = LoadTexture("Images/button_close");
		ButtonHelp = LoadTexture("Images/button_help");
		ButtonPin = LoadTexture("Images/button_pin");
		ButtonFullscreen = LoadTexture("Images/button_fullscreen");

		PanelSearchBar = LoadTexture("Images/panel_search_bar");
	}

	private static Asset<Texture2D> LoadTexture(string path)
	{
		/*
		 * This has to be loaded in immediate mode so that texture dimensions are guaranteed to be
		 * correct.
		 */
		return QuiteEnoughRecipes.Instance.Assets.Request<Texture2D>(path,
				AssetRequestMode.ImmediateLoad);
	}
}
