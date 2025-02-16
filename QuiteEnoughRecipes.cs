using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using ReLogic.Content;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

public class QuiteEnoughRecipes : Mod
{
	public static void DrawItemIcon(Item item, int context, SpriteBatch spriteBatch,
		Vector2 screenPositionForItemCenter, float scale, float sizeLimit, Color environmentColor)
	{
		LoadItemAsync(item.type);
		ItemSlot.DrawItemIcon(item, context, spriteBatch, screenPositionForItemCenter, scale, sizeLimit, environmentColor);
	}

	public static void LoadItemAsync(int i)
	{
		if (TextureAssets.Item[i].State == AssetState.NotLoaded)
		{
			Main.Assets.Request<Texture2D>(TextureAssets.Item[i].Name, AssetRequestMode.AsyncLoad);
		}
	}

	public static void LoadTileAsync(int tileId)
	{
		if (TextureAssets.Tile[tileId].State == AssetState.NotLoaded)
		{
			Main.Assets.Request<Texture2D>(TextureAssets.Tile[tileId].Name, AssetRequestMode.AsyncLoad);
		}
	}

	public static void LoadWallAsync(int wallId)
	{
		if (TextureAssets.Wall[wallId].State == AssetState.NotLoaded)
		{
			Main.Assets.Request<Texture2D>(TextureAssets.Wall[wallId].Name, AssetRequestMode.AsyncLoad);
		}
	}
}
