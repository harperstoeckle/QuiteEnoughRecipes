using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace QuiteEnoughRecipes;

/*
 * To be placed at the top of a floating window. Has a title on the left side and some elements on
 * the right side (usually at least a close button).
 */
public class UITitleBar : UIPanel
{
	private class UIHelpIcon : UIQERButton
	{
		private LocalizedText _text;

		public UIHelpIcon(LocalizedText helpText) : base(QERAssets.ButtonHelp)
		{
			_text = helpText;
		}

		protected override void DrawSelf(SpriteBatch sb)
		{
			base.DrawSelf(sb);
			if (IsMouseHovering)
			{
				UICommon.TooltipMouseText(_text.Value);
			}
		}
	}

	public const float BarInnerPadding = 10;
	public const float BarOuterPadding = 6;
	public const float BarItemWidth = 22;
	public const float BarItemHeight = BarItemWidth;
	public const float BarHeight = BarItemHeight + 2 * BarOuterPadding;
	public const float TitleWidth = 100;

	private UIText _title;
	private StyleDimension _rightOffset = StyleDimension.Empty;

	public UITitleBar(LocalizedText title)
	{
		Width = StyleDimension.Fill;
		Height = new(BarHeight, 0);
		SetPadding(BarOuterPadding);
		BackgroundColor = QERColors.Brown;
		BorderColor = QERColors.DarkBrown;

		_title = new(title){
			Width = new(TitleWidth, 0),
			Height = StyleDimension.Fill,
			ShadowColor = Color.Transparent,
			DynamicallyScaleDownToWidth = true,
			TextOriginX = 0,
		};

		Append(_title);
	}

	// Elements are added from right to left, on the right side.
	public void AddElement(UIElement e)
	{
		_rightOffset = _rightOffset.Plus(e.Width);
		e.Left = StyleDimension.Fill.Minus(_rightOffset);
		_rightOffset = _rightOffset.Plus(new(BarInnerPadding, 0));

		Append(e);
	}

	public void AddHelp(LocalizedText helpText) => AddElement(new UIHelpIcon(helpText));
}

/*
 * If we have two `StyleDimension`s `a` and `b` laid out side-by-side with the same parent, then
 * their total width can be expressed by a new `StyleDimension` whose `Pixels` is the sum of the
 * `Pixels` of `a` and `b`, and whose `Percent` is the sum of the percents of `a` and `b`. In
 * other words, being able to "add" and "subtract" `StyleDimension`s is meaningful and useful.
 */
static file class SyleDimensionExtensions
{
	public static StyleDimension Plus(this StyleDimension dim, StyleDimension other)
	{
		return new(dim.Pixels + other.Pixels, dim.Percent + other.Percent);
	}

	public static StyleDimension Minus(this StyleDimension dim, StyleDimension other)
	{
		return dim.Plus(other.Negate());
	}

	public static StyleDimension Negate(this StyleDimension dim)
	{
		return new(-dim.Pixels, -dim.Percent);
	}
}
