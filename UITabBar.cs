using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

/*
 * Provides a horizontal set of tabs that can be clicked through, where each tab keeps track of a
 * value of type `T`. Note that `UITabBar` doesn't actually handle the content switching; this
 * should be handled by subscribing to `OnTabSelected`.
 */
public class UITabBar<T> : UIElement
{
	private class Tab : UIPanel
	{
		private static readonly Color SelectedBackgroundColor = Color.White * 0.7f;
		private static readonly Color UnselectedBackgroundColor = new Color(63, 82, 151) * 0.7f;

		private LocalizedText _text;

		public bool Selected = false;
		public T  Value { get; private set; }

		public Tab(LocalizedText text, Item item, T value)
		{
			_text = text;
			Value = value;
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

	/*
	 * Called as `OnTabSelected(v)` when a tab is selected, where `v` is the value associated with
	 * the tab.
	 */
	public event Action<T> OnTabSelected;

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
	 * Add a tab. The tab itself will use `item` as an icon and will display `text` when hovered.
	 * This tab will be associated with the value `v`; when this tab is clicked, `OnTabSelected`
	 * will be activated with `v` as the argument.
	 */
	public int AddTab(LocalizedText text, Item item, T v)
	{
		var tab = new Tab(text, item, v);
		_tabs.Add(tab);
		Append(tab);
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
			MakeTabCurrent(t);
		}
	}

	/*
	 * Switch to the tab associated with the value `v`. This *will* activate the `OnTabSelected`
	 * event. If multiple tabs have the value `v`, then the leftmost one will always be chosen.
	 */
	public bool OpenTabFor(T v)
	{
		var tab = _tabs.FirstOrDefault(t => Object.Equals(t.Value, v));
		if (tab == null) { return false; }
		MakeTabCurrent(tab);
		return true;
	}

	private void MakeTabCurrent(Tab t)
	{
		foreach (var tab in _tabs) { tab.Selected = false; }
		t.Selected = true;
		OnTabSelected?.Invoke(t.Value);
	}
}
