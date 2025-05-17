using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using ReLogic.Content;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

/*
 * This is just a blatant copy of the bestiary search bar, but it looks pretty good and is visually
 * cohesive, so that's fine I think.
 */
public class UIQERSearchBar : UIPanel
{
	private static readonly Color _backgroundColor = new(35, 40, 83);

	private static UIQERSearchBar? _activeInstance = null;
	private UISearchBar _search;

	/*
	 * We just want to treat this element as if it's the search bar itself, so we forward event
	 * subscriptions to the search bar.
	 */
	public event Action OnCanceledTakingInput
	{
		add { _search.OnCanceledTakingInput += value; }
		remove { _search.OnCanceledTakingInput -= value; }
	}
	public event Action<string> OnContentsChanged
	{
		add { _search.OnContentsChanged += value; }
		remove { _search.OnContentsChanged -= value; }
	}
	public event Action OnEndTakingInput
	{
		add { _search.OnEndTakingInput += value; }
		remove { _search.OnEndTakingInput -= value; }
	}
	public event Action OnStartTakingInput
	{
		add { _search.OnStartTakingInput += value; }
		remove { _search.OnStartTakingInput -= value; }
	}

	public UIQERSearchBar() : base(QERAssets.PanelSearchBar, QERAssets.PanelSearchBar, 8, 4)
	{
		OverflowHidden = true;
		Width.Percent = 1;
		Height.Pixels = 24;
		SetPadding(0);
		BackgroundColor = Color.White;
		BorderColor = Color.Transparent;

		_search = new UISearchBar(Language.GetText("Mods.QuiteEnoughRecipes.UI.SearchBarDefault"), 0.8f){
			VAlign = 0.5f,
			IgnoresMouseInteraction = true
		};

		// Needed to ensure the search bar starts with the faded text.
		Clear();

		_search.OnStartTakingInput += () => {
			_activeInstance?.SetTakingInput(false);
			_activeInstance = this;
		};
		_search.OnEndTakingInput += () => {
			_activeInstance = null;
		};

		Append(_search);
	}

	public override void LeftClick(UIMouseEvent e)
	{
		base.LeftClick(e);
		_search.ToggleTakingText();
	}

	// Right clicking clears the text and starts taking input.
	public override void RightClick(UIMouseEvent e)
	{
		base.RightClick(e);
		Clear();
		SetTakingInput(true);
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		base.DrawSelf(sb);

		if (IsMouseHovering)
		{
			UISystem.CustomCursorTexture = QERAssets.CursorIBeam;
			UISystem.CustomCursorOffset = QERAssets.CursorIBeam.Frame().Size() / 2;
		}
	}

	public void SetTakingInput(bool b)
	{
		if (_search.IsWritingText != b)
		{
			_search.ToggleTakingText();
		}
	}

	public void Clear()
	{
		_search.SetContents("");
	}

	public static void UnfocusAll()
	{
		_activeInstance?.SetTakingInput(false);
	}
}
