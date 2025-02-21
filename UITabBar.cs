using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

/*
 * Provides a horizontal set of tabs that can be clicked through. `UITabBar` does *not* handle
 * actually switching the contents; this should be done by subscribing to the `OnTabSelected` event.
 */
public class UITabBar : UIElement
{
	private class Tab : UIPanel
	{
		private static readonly Color SelectedBackgroundColor = Color.White * 0.7f;
		private static readonly Color UnselectedBackgroundColor = new Color(63, 82, 151) * 0.7f;

		private LocalizedText _text;

		public bool Selected = false;
		public int Index { get; private set; }

		public Tab(LocalizedText text, Item item, int index)
		{
			_text = text;
			Index = index;
			Append(new UIItemIcon(item, false){ IgnoresMouseInteraction = true, VAlign = 0.5f });
		}

		protected override void DrawSelf(SpriteBatch sb)
		{
			BackgroundColor = Selected ? SelectedBackgroundColor : UnselectedBackgroundColor;

			if (IsMouseHovering)
			{
				Main.instance.MouseText(_text.Value);
			}

			base.DrawSelf(sb);
		}
	}

	private List<Tab> _tabs = new();

	// Called with the index of the tab that was selected.
	public event Action<int> OnTabSelected;

	public UITabBar()
	{
		OverflowHidden = true;
	}

	public void ClearTabs()
	{
		_tabs.Clear();
		RemoveAllChildren();
		Recalculate();
	}

	/*
	 * Adds a tab to the right that displays `text` when hovered and returns its index. The tab
	 * itself will display `item` as an icon.
	 */
	public int AddTab(LocalizedText text, Item item)
	{
		var tab = new Tab(text, item, _tabs.Count);
		_tabs.Add(tab);
		Append(tab);

		// This is the first tab, so we want to select it by default.
		if (_tabs.Count == 1)
		{
			tab.Selected = true;
		}

		Recalculate();
		return _tabs.Count - 1;
	}

	private void ReLayoutTabs()
	{
		const float padding = 5;
		float tabWidth = 42;

		for (int i = 0; i < _tabs.Count; ++i)
		{
			/*
			 * The panel needs to be a bit taller than the tab bar so that it hangs off of the
			 * bottom. Since `OverflowHidden` is set, the bottom of the panel will be invisible,
			 * which gives it a more "tab-like" feel.
			 */
			_tabs[i].Height = _tabs[i].MaxHeight = new StyleDimension(10, 1);
			_tabs[i].VAlign = 0;
			_tabs[i].Width.Pixels = tabWidth;
			_tabs[i].Left.Pixels = i * (padding + tabWidth);
		}
	}

	public override void RecalculateChildren()
	{
		ReLayoutTabs();
		base.RecalculateChildren();
	}

	public override void LeftClick(UIMouseEvent e)
	{
		// I'm not sure if this is the "correct" way to do things.
		if (e.Target is Tab t)
		{
			SwitchToTab(t.Index);
		}
	}

	// This will trigger the `OnTabSelected` event.
	public void SwitchToTab(int i)
	{
		if (i >= _tabs.Count) { return; }
		foreach (var tab in _tabs) { tab.Selected = false; }
		_tabs[i].Selected = true;
		OnTabSelected?.Invoke(i);
	}
}
