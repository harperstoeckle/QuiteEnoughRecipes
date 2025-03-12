using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader.UI;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

public interface IQueryable<T>
{
	public void SetSearchText(string text);

	/*
	 * These will be added to the filter and sort panels. Actually detecting changes in them and
	 * doing the sorting/filtering has to be handled entirely by the class implementing
	 * `IQueryable`.
	 */
	public IEnumerable<IOptionGroup> GetFilterGroups();
	public IEnumerable<IOptionGroup> GetSortGroups();
}

// A page containing a search bar that can automatically be focused.
public interface IFocusableSearchPage
{
	public void FocusSearchBar();
}

public class OptionPanelToggleButton : UIElement
{

	/*
	 * This will be displayed as a popup when the button is pressed. If it is not in its default
	 * state, then this button will show a small red dot indicator. If this button is right clicked,
	 * then this option group will be set to its default state.
	 */
	public required IOptionGroup OptionGroup;
	public required string IconPath;
	public required string Name;

	/*
	 * `image` is supposed to be either the bestiary filtering or sorting button, since the icon
	 * can be easily cut out of them from a specific location.
	 */
	public OptionPanelToggleButton()
	{
		Width.Pixels = Height.Pixels = 22;
	}

	public override void LeftClick(UIMouseEvent e)
	{
		base.LeftClick(e);
		UISystem.UI?.OpenPopup(OptionGroup.Element);
	}

	public override void RightClick(UIMouseEvent e)
	{
		base.RightClick(e);
		if (Main.keyState.PressingShift()) { OptionGroup.ClearLocks(); }
		OptionGroup.Reset();
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		base.DrawSelf(sb);

		var pos = GetDimensions().Position();
		var filterIcon = Main.Assets.Request<Texture2D>(IconPath).Value;

		// We only want the icon part of the texture without the part that usually has the text.
		sb.Draw(filterIcon, pos, new Rectangle(4, 4, 22, 22), Color.White);

		bool isSelected = !OptionGroup.IsDefaulted;
		bool hasLock = OptionGroup.HasLocks;

		/*
		 * This is the same indicator used for trapped chests. It's about the right shape and
		 * size, so it's good enough.
		 */
		if (isSelected)
		{
			sb.Draw(TextureAssets.Wire.Value, pos + new Vector2(0, 4),
				new Rectangle(4, 58, 8, 8), Color.White, 0f, new Vector2(4), 1, 0, 0);
		}

		if (hasLock)
		{
			sb.Draw(TextureAssets.Wire2.Value, pos + new Vector2(6, 4),
				new Rectangle(4, 58, 8, 8), Color.White, 0f, new Vector2(4), 1, 0, 0);
		}

		if (IsMouseHovering)
		{
			var c = Language.GetTextValue("Mods.QuiteEnoughRecipes.UI.RightClickToClear");
			var l = Language.GetTextValue("Mods.QuiteEnoughRecipes.UI.ShiftRightClickToClearLocks");
			Main.instance.MouseText(Name + (isSelected ? $"\n{c}" : "") + (hasLock ? $"\n{l}" : ""));
		}
	}
}

/*
 * A page with a search bar, filters, and sorting options. It maintains an `IQueryable` element,
 * updating it whenever the search options change.
 */
public class UISearchPage<T> : UIElement, IFocusableSearchPage
{
	// Icon that provides help when hovered.
	private class HelpIcon : UIElement
	{
		private LocalizedText _text;
		public HelpIcon(LocalizedText text)
		{
			_text = text;

			Width.Pixels = Height.Pixels = 22;
			Append(new UIText("?", 0.8f){
				HAlign = 0.5f,
				VAlign = 0.5f,
				TextColor = new Color(0.7f, 0.7f, 0.7f)
			});
		}

		protected override void DrawSelf(SpriteBatch sb)
		{
			// Kind of a silly way to draw the text on top.
			base.DrawSelf(sb);

			if (IsMouseHovering)
			{
				UICommon.TooltipMouseText(_text.Value);
			}
		}
	}

	private IQueryable<T> _queryable;


	private UIOptionPanel _filterPanel = new();
	private UIOptionPanel _sortPanel = new();
	private UIQERSearchBar _searchBar = new();

	/*
	 * `squareSideLength` is the side length of the grid squares, and `padding` is the amount of
	 * padding between grid squares.
	 */
	public UISearchPage(IQueryable<T> queryElement, LocalizedText helpText)
	{
		const float BarHeight = 50;

		_queryable = queryElement;

		Width.Percent = 1;
		Height.Percent = 1;

		float offset = 0;

		var filters = queryElement.GetFilterGroups().ToList();
		if (filters.Count > 0)
		{
			_filterPanel.Width.Percent = 1;
			_filterPanel.Height.Percent = 1;
			foreach (var f in filters) { _filterPanel.AddGroup(f); }

			var filterToggleButton = new OptionPanelToggleButton{
				IconPath = "Images/UI/Bestiary/Button_Filtering",
				Name = Language.GetTextValue("Mods.QuiteEnoughRecipes.UI.FilterHover"),
				OptionGroup = _filterPanel,
				Left = new(offset, 0)
			};

			offset += filterToggleButton.Width.Pixels + 10;
			Append(filterToggleButton);
		}

		var sorts = queryElement.GetSortGroups().ToList();
		if (sorts.Count > 0)
		{
			_sortPanel.Width.Percent = 1;
			_sortPanel.Height.Percent = 1;
			foreach (var s in sorts) { _sortPanel.AddGroup(s); }

			var sortToggleButton = new OptionPanelToggleButton{
				IconPath = "Images/UI/Bestiary/Button_Sorting",
				Name = Language.GetTextValue("Mods.QuiteEnoughRecipes.UI.SortHover"),
				OptionGroup = _sortPanel,
				Left = new(offset, 0)
			};

			offset += sortToggleButton.Width.Pixels + 10;
			Append(sortToggleButton);
		}

		var helpIcon = new HelpIcon(helpText);
		helpIcon.HAlign = 1;

		// Make room for the filters on the left and the help icon on the right.
		_searchBar.Width = new StyleDimension(-offset - helpIcon.Width.Pixels - 10, 1);
		_searchBar.Left.Pixels = offset;
		_searchBar.OnContentsChanged += s => {
			_queryable.SetSearchText(s ?? "");
		};

		ApplyDefaults();

		/*
		 * There aren't generic constructors, so there's no way to require that `queryElement` be
		 * a `UIElement`, so we have to do that check here.
		 */
		if (queryElement is UIElement e)
		{
			e.Width.Percent = 1;
			e.Height = new StyleDimension(-BarHeight, 1);
			e.VAlign = 1;

			Append(e);
		}

		Append(_searchBar);
		Append(helpIcon);
	}

	public void FocusSearchBar()
	{
		_searchBar.SetTakingInput(true);
	}

	public void ApplyDefaults()
	{
		_filterPanel.Reset();
		_sortPanel.Reset();
		_searchBar.Clear();
	}
}
