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
		private static readonly Color SelectedBorderColor = Main.OurFavoriteColor;
		private static readonly Color UnselectedBorderColor = Color.Black;

		private LocalizedText _text;

		public bool Selected = false;
		public int Index { get; private set; }

		public Tab(LocalizedText text, int index)
		{
			_text = text;
			Index = index;
		}

		protected override void DrawSelf(SpriteBatch sb)
		{
			BorderColor = Selected ? SelectedBorderColor : UnselectedBorderColor;

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

	public void ClearTabs()
	{
		_tabs.Clear();
		RemoveAllChildren();
		Recalculate();
	}

	// Adds a tab to the right that displays `text` when hovered and returns its index.
	public int AddTab(LocalizedText text)
	{
		var tab = new Tab(text, _tabs.Count);
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
		float tabWidth = (GetInnerDimensions().Width - padding * (_tabs.Count - 1)) / _tabs.Count;

		for (int i = 0; i < _tabs.Count; ++i)
		{
			_tabs[i].Height.Percent = 1.0f;
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
			foreach (var tab in _tabs) { tab.Selected = false; }
			t.Selected = true;
			OnTabSelected?.Invoke(t.Index);
		}
	}
}
