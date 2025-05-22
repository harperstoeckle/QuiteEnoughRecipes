using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Linq;
using System.Reflection;
using System;
using Terraria.ModLoader;

namespace QuiteEnoughRecipes;

public class QERAssets : ModSystem
{
	[AttributeUsage(AttributeTargets.Field)]
	private class AutoTextureAttribute(string path) : Attribute
	{
		public string Path = path;
	}

	// These should never be null when they're used, since they're loaded in `Load`.
#nullable disable
	[AutoTexture("Images/cursor_corner_right")] public static Asset<Texture2D> CursorCornerRight;
	[AutoTexture("Images/cursor_corner_left")] public static Asset<Texture2D> CursorCornerLeft;
	[AutoTexture("Images/cursor_edge_horizontal")] public static Asset<Texture2D> CursorEdgeHorizontal;
	[AutoTexture("Images/cursor_edge_vertical")] public static Asset<Texture2D> CursorEdgeVertical;
	[AutoTexture("Images/cursor_i_beam")] public static Asset<Texture2D> CursorIBeam;

	[AutoTexture("Images/button_close")] public static Asset<Texture2D> ButtonClose;
	[AutoTexture("Images/button_help")] public static Asset<Texture2D> ButtonHelp;
	[AutoTexture("Images/button_pin")] public static Asset<Texture2D> ButtonPin;
	[AutoTexture("Images/button_fullscreen")] public static Asset<Texture2D> ButtonFullscreen;

	[AutoTexture("Images/panel_search_bar")] public static Asset<Texture2D> PanelSearchBar;

	[AutoTexture("Images/inventory_background")] public static Asset<Texture2D> InventoryBackground;
#nullable enable

	public override void Load()
	{
		/*
		 * Extremely lazy way to automatically load texture. It's probably highly unnecessary, but
		 * it makes it easier to avoid accidentally not initializing a texture.
		 */
		var textureFields = typeof(QERAssets)
			.GetFields(BindingFlags.Public | BindingFlags.Static)
			.Where(f => f.FieldType == typeof(Asset<Texture2D>));
		foreach (var field in textureFields)
		{
			var textureAttr = field.GetCustomAttribute<AutoTextureAttribute>();
			if (textureAttr is null) { continue; }

			field.SetValue(null, LoadTexture(textureAttr.Path));
		}
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
