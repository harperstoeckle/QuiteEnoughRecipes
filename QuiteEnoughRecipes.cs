using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using ReLogic.Content;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using Terraria.GameContent.UI;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

public class QuiteEnoughRecipes : Mod
{
	public static void DrawItemIcon(Item item, int context, SpriteBatch spriteBatch,
		Vector2 screenPositionForItemCenter, float scale, float sizeLimit, Color environmentColor)
	{
		if (LoadItemAsync(item.type).IsLoaded)
		{
			ItemSlot.DrawItemIcon(item, context, spriteBatch, screenPositionForItemCenter, scale, sizeLimit, environmentColor);
		}
	}

	public static Asset<Texture2D> LoadTextureAsync(Asset<Texture2D> asset)
	{
		if (asset.State == AssetState.NotLoaded)
		{
			return Main.Assets.Request<Texture2D>(asset.Name, AssetRequestMode.AsyncLoad);
		}
		return asset;
	}

	public static Asset<Texture2D> LoadItemAsync(int i) => 
		LoadTextureAsync(TextureAssets.Item[i]);

	public static Asset<Texture2D> LoadNPCAsync(int i) =>
		LoadTextureAsync(TextureAssets.Npc[i]);

	/*
	 * When displaying the name of an ingredient that comes from a mod, this should be appended
	 * immediately after the name so that it's clear what mod the ingredient came from.
	 */
	public static string GetModTagText(Mod mod) =>
		AddColorTagSafe($"   〈{mod.DisplayNameClean}〉", new Color(0x56, 0x66, 0x5e));

	// Get text for the buy price of an item. This supports custom currencies.
	public static string GetBuyPriceText(Item item)
	{
		int storeValue = item.GetStoreValue();

		if (storeValue <= 0)
		{
			// No value.
			var color = Colors.AlphaDarken(new Color(120, 120, 120));
			return $"[c/{color.Hex3()}:{Lang.tip[51]}]";
		}
		else if (item.shopSpecialCurrency != -1)
		{
			var line = new string[1];
			var curLine = 0;
			CustomCurrencyManager.GetPriceText(item.shopSpecialCurrency, line, ref curLine,
				storeValue);
			return line[0];
		}
		else
		{
			return $"{Lang.tip[50]} {GetCoinValueText(storeValue)}";
		}
	}

	/*
	 * Creates colored coin text based on a given value as seen when reforging. Taken from
	 * `Main.DrawInventory()`.
	 */
	public static string GetCoinValueText(int value)
	{
		var text = new StringBuilder();
		var coins = Utils.CoinsSplit(value);
		var copper = coins[0];
		var silver = coins[1];
		var gold = coins[2];
		var platinum = coins[3];

		void AddCoinText(Color color, int amount, int langIndex) =>
			text.Append($"[c/{color.Hex3()}:{amount} {Lang.inter[langIndex].Value}] ");

		if (platinum > 0)
			AddCoinText(Colors.AlphaDarken(Colors.CoinPlatinum), platinum, 15);

		if (gold > 0)
			AddCoinText(Colors.AlphaDarken(Colors.CoinGold), gold, 16);

		if (silver > 0)
			AddCoinText(Colors.AlphaDarken(Colors.CoinSilver), silver, 17);

		if (copper > 0)
			AddCoinText(Colors.AlphaDarken(Colors.CoinCopper), copper, 18);

		return text.ToString();
	}

	/*
	 * Make a tagged string in color `c`. `s` *may* contain brackets; they will also be
	 * color-coded appropriately.
	 */
	public static string AddColorTagSafe(string s, Color c)
	{
		s = s.Replace("]", $"][c/{c.Hex3()}:]");
		return $"[c/{c.Hex3()}:{s}]";
	}
}
