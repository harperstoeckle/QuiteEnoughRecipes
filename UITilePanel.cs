using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent;
using Terraria.ObjectData;
using Terraria.UI;

namespace QuiteEnoughRecipes;
public class UITilePanel : UIElement
{
	public int TileId;
	public int TileStyle;

	public UITilePanel(int tileId, int style)
	{
		TileId = tileId;
		TileStyle = style;

		Width.Pixels = 50;
		Height.Pixels = 50;
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		base.DrawSelf(spriteBatch);
		DrawTile(spriteBatch);
	}

	private void DrawTile(SpriteBatch spriteBatch)
	{
		QuiteEnoughRecipes.LoadTileAsync(TileId);

		if (Main.tileFrameImportant[TileId])
		{
			var tileData = TileObjectData.GetTileData(TileId, TileStyle);
			if (tileData != null)
			{
				DrawMultiTile(spriteBatch, tileData);
			}
			else
			{
				DrawSingleTile(spriteBatch, new(0, 0));
			}
		}
		else
		{
			DrawSingleTile(spriteBatch, new(9, 3));
		}
		
	}

	private void DrawSingleTile(SpriteBatch spriteBatch, Point tileOnSheet)
	{
		var texture = TextureAssets.Tile[TileId];
		var dimensions = GetInnerDimensions();

		// Fixes blurry textures
		RasterizerState rasterizerState = spriteBatch.GraphicsDevice.RasterizerState;
		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, rasterizerState, null, Main.UIScaleMatrix);

		var size = 16;
		var padding = 2;
		var x = tileOnSheet.X * (size + padding);
		var y = tileOnSheet.Y * (size + padding);

		spriteBatch.Draw(texture.Value, dimensions.Center(), new(x, y, size, size), Color.White, 0f, Vector2.One * (size / 2), 2, SpriteEffects.None, 0);
	}

	/*
	 * Adapted from TileDefinitionOptionElement.DrawMultiTile(), but takes style information into account
	 */
	private void DrawMultiTile(SpriteBatch spriteBatch, TileObjectData tileData)
	{
		var dimensions = GetInnerDimensions();

		// Fixes gaps in textures
		RasterizerState rasterizerState = spriteBatch.GraphicsDevice.RasterizerState;
		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, rasterizerState, null, Main.UIScaleMatrix);

		Vector2 positionTopLeft = dimensions.Position() + new Vector2(4, 4);
		float drawDimensionHeight = dimensions.Height - 8;
		float drawDimensionWidth = dimensions.Width - 8;
		// changing to Max fixes gaps in crates and chests but breaks most other things, there is something fishy here
		float drawScale = Math.Min(drawDimensionWidth / (tileData.CoordinateWidth * tileData.Width), drawDimensionHeight / tileData.CoordinateHeights.Sum());
		float adjustX = tileData.Width < tileData.Height ? (tileData.Height - tileData.Width) / (tileData.Height * 2f) : 0f;
		float adjustY = tileData.Height < tileData.Width ? (tileData.Width - tileData.Height) / (tileData.Width * 2f) : 0f;

		Texture2D tileTexture = TextureAssets.Tile[TileId].Value;
		int placeStyle = tileData.CalculatePlacementStyle(TileStyle, 0, 0);
		int row = 0;
		int drawYOffset = tileData.DrawYOffset;
		int drawXOffset = tileData.DrawXOffset;
		placeStyle += tileData.DrawStyleOffset;
		int styleWrapLimit = tileData.StyleWrapLimit;
		int styleLineSkip = tileData.StyleLineSkip;
		if (tileData.StyleWrapLimitVisualOverride.HasValue)
			styleWrapLimit = tileData.StyleWrapLimitVisualOverride.Value;

		if (tileData.styleLineSkipVisualOverride.HasValue)
			styleLineSkip = tileData.styleLineSkipVisualOverride.Value;

		if (styleWrapLimit > 0)
		{
			row = placeStyle / styleWrapLimit * styleLineSkip;
			placeStyle %= styleWrapLimit;
		}

		int topLeftX;
		int topLeftY;
		if (tileData.StyleHorizontal)
		{
			topLeftX = tileData.CoordinateFullWidth * placeStyle;
			topLeftY = tileData.CoordinateFullHeight * row;
		}
		else
		{
			topLeftX = tileData.CoordinateFullWidth * row;
			topLeftY = tileData.CoordinateFullHeight * placeStyle;
		}

		int tileWidth = tileData.Width;
		int tileHeight = tileData.Height;
		int maxTileDimension = Math.Max(tileData.Width, tileData.Height);

		for (int i = 0; i < tileWidth; i++)
		{
			int x = topLeftX + i * (tileData.CoordinateWidth + tileData.CoordinatePadding);
			int y = topLeftY;
			for (int j = 0; j < tileHeight; j++)
			{
				if (j == 0 && tileData.DrawStepDown != 0)
					drawYOffset += tileData.DrawStepDown;

				if (TileId == 567)
					drawYOffset = (j != 0) ? tileData.DrawYOffset : (tileData.DrawYOffset - 2);

				int drawWidth = tileData.CoordinateWidth;
				int drawHeight = tileData.CoordinateHeights[j];
				if (TileId == 114 && j == 1)
					drawHeight += 2;

				spriteBatch.Draw(
					sourceRectangle: new Rectangle(x, y, drawWidth, drawHeight),
					texture: tileTexture,
					position: new Vector2(
						positionTopLeft.X + ((float)i / maxTileDimension + adjustX) * drawDimensionWidth,
						positionTopLeft.Y + ((float)j / maxTileDimension + adjustY) * drawDimensionHeight
					),
					color: Color.White, rotation: 0f, origin: Vector2.Zero, scale: drawScale, effects: SpriteEffects.None, layerDepth: 0f);
				y += drawHeight + tileData.CoordinatePadding;
			}
		}
	}
}
