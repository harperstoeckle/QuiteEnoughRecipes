using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using ReLogic.Content;
using System;
using Terraria.Localization;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

public class UIQERButton : UIElement
{
	private const float HoveredBrightness = 1.0f;
	private const float NotHoveredBrightness = 0.8f;

	private Asset<Texture2D> _texture;
	private int _numFrames;

	public LocalizedText? HoverText = null;

	public int Frame = 0;

	// If set, this frame will be drawn when the button is disabled.
	public int? DisabledFrame = null;

	// The button will not react when hovered.
	public bool IsDisabled = false;

	public UIQERButton(Asset<Texture2D> texture, int numFrames = 1)
	{
		_texture = texture;
		_numFrames = Math.Max(1, numFrames);

		Width.Pixels = _texture.Width();
		Height.Pixels = _texture.Height();
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		base.DrawSelf(sb);

		float brightness = IsMouseHovering && !IsDisabled ? HoveredBrightness : NotHoveredBrightness;
		int frame = IsDisabled && DisabledFrame is not null ? DisabledFrame.Value : Frame;

		sb.Draw(_texture.Value, GetDimensions().Position(), _texture.Frame(_numFrames, 1, frame),
				Color.White * brightness);

		if (IsMouseHovering && HoverText is not null)
		{
			Main.instance.MouseText(HoverText.Value);
		}
	}
}
