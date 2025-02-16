using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace QuiteEnoughRecipes;
public class UIWallPanel : UIElement
{
	public int WallId { get; set; }

	public int Border { get; set; }
	public string HoverText { get; set; } = "";

	public UIWallPanel(int wallId, int size = 50)
	{
		WallId = wallId;
		HoverText = WallID.Search.GetName(wallId);
		Border = 8;

		Width.Pixels = size;
		Height.Pixels = size;
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		base.DrawSelf(spriteBatch);
		DrawWall(spriteBatch);
	}

	private void DrawWall(SpriteBatch spriteBatch)
	{
		QuiteEnoughRecipes.LoadWallAsync(WallId);

		if (WallId > WallID.None && WallId < WallLoader.WallCount)
		{
			var startPos = new Point(324, 108);
			DrawWall(spriteBatch, startPos);
		}
		
		if (IsMouseHovering)
		{
			Main.instance.MouseText(HoverText);
		}
	}

	private void DrawWall(SpriteBatch spriteBatch, Point startPos)
	{
		var texture = TextureAssets.Wall[WallId];
		var dimensions = GetInnerDimensions();

		// Fixes blurry textures
		RasterizerState rasterizerState = spriteBatch.GraphicsDevice.RasterizerState;
		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, rasterizerState, null, Main.UIScaleMatrix);

		var size = 32;
		float drawScale = Math.Min((dimensions.Width - Border) / size, (dimensions.Height - Border) / size);

		spriteBatch.Draw(texture.Value, dimensions.Center(), new(startPos.X, startPos.Y, size, size), Color.White, 0f, Vector2.One * (size / 2), drawScale, SpriteEffects.None, 0);
	}
}
