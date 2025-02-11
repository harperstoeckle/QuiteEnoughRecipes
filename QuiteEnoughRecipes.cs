using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace QuiteEnoughRecipes;

public class QuiteEnoughRecipes : Mod
{
    public static void DrawItemIcon(Item item, int context, SpriteBatch spriteBatch, Vector2 screenPositionForItemCenter, float scale, float sizeLimit, Color environmentColor)
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
}
