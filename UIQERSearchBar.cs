using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
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

	public UIQERSearchBar()
	{
		_search = new UISearchBar(Language.GetText("Mods.QuiteEnoughRecipes.UI.SearchBarDefault"), 0.8f){
			VAlign = 0.5f,
			IgnoresMouseInteraction = true
		};

		// Needed to ensure the search bar starts with the faded text.
		_search.SetContents(null, true);

		/*
		 * TODO: Is there a better way to do this?
		 *
		 * For some reason, `UISearchBar` will sometimes fail to block input if
		 * the game is running too slowly while typing, i.e., if it takes too
		 * long to filter the results when the search changes (I'm not sure if
		 * this is an 100% accurate characterization of the bug, admittedly).
		 * Setting `blockInput` like this prevents that from happening.
		 */
		_search.OnStartTakingInput += () => {
			_activeInstance?.SetTakingInput(false);
			Main.blockInput = true;
			_activeInstance = this;
		};
		_search.OnEndTakingInput += () => {
			_activeInstance = null;
			Main.blockInput = false;
		};

		Width.Percent = 1;
		Height.Pixels = 24;
		SetPadding(0);

		BackgroundColor = _backgroundColor;
		BorderColor = _backgroundColor;

		Append(_search);
	}

	public override void OnDeactivate()
	{
		/*
		 * This ensures that if, for some reason, the UI is closed without
		 * unfocusing the search bar, input will stop being blocked. Otherwise,
		 * the player can be stuck unable to do any inputs.
		 */
		Main.blockInput = false;
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
		_search.SetContents("");
		SetTakingInput(true);
	}

	public void SetTakingInput(bool b)
	{
		if (_search.IsWritingText != b)
		{
			_search.ToggleTakingText();
		}
	}

	public static void UnfocusAll()
	{
		_activeInstance?.SetTakingInput(false);
	}
}
