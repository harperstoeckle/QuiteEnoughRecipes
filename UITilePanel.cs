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
using Terraria.ObjectData;
using Terraria.UI;

namespace QuiteEnoughRecipes;
public class UITilePanel : UIElement
{
	public int TileId { get; set; }
	public int TileStyle { get; set; }

	public int Border { get; set; }
	public string HoverText { get; set; } = "";

	public UITilePanel(int tileId, int style, int size = 50)
	{
		TileId = tileId;
		TileStyle = style;
		HoverText = TileID.Search.GetName(tileId);
		Border = 8;

		Width.Pixels = size;
		Height.Pixels = size;
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
			// (9, 3), (9, 4), (9, 5) are all the single tile options
			var spritePos = new Point(9, 3);
			DrawSingleTile(spriteBatch, spritePos);
		}
		
		if (IsMouseHovering)
		{
			Main.instance.MouseText(HoverText);
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
		float drawScale = Math.Min((dimensions.Width - Border) / size, (dimensions.Height - Border) / size);

		spriteBatch.Draw(texture.Value, dimensions.Center(), new(x, y, size, size), Color.White, 0f, Vector2.One * (size / 2), drawScale, SpriteEffects.None, 0);
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

		var border = Border;
		Vector2 positionTopLeft = dimensions.Position() + new Vector2(border / 2, border / 2);
		float drawDimensionHeight = dimensions.Height - border;
		float drawDimensionWidth = dimensions.Width - border;
		
		/*
		 * Tiles that have varying CoordinateHeights have gaps in their sprite when drawn
		 * Increasing the scale by a small amount over scales the image and removes the gaps
		 * TODO: figure out exactly why this is happening and implement a real solution
		 */
		float scaleFix = 0.1f;
		float drawScale = Math.Min(drawDimensionWidth / (tileData.CoordinateWidth * tileData.Width), drawDimensionHeight / tileData.CoordinateHeights.Sum()) + scaleFix;
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

				var sourceRectangle = new Rectangle(x, y, drawWidth, drawHeight);
				var position = new Vector2(
						positionTopLeft.X + ((float)i / maxTileDimension + adjustX) * drawDimensionWidth,
						positionTopLeft.Y + ((float)j / maxTileDimension + adjustY) * drawDimensionHeight
				);

				spriteBatch.Draw(
					sourceRectangle: sourceRectangle,
					texture: tileTexture,
					position: position,
					color: Color.White, rotation: 0f, origin: Vector2.Zero, scale: drawScale, effects: SpriteEffects.None, layerDepth: 0f);
				y += drawHeight + tileData.CoordinatePadding;
			}
		}
	}
}
