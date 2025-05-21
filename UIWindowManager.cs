using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria.UI;

namespace QuiteEnoughRecipes;

/*
 * Maintains a set of windows and facilitates interaction between them. For example, it allows
 * windows to be opened and closed or moved to the top.
 */
public class UIWindowManager : UIState
{
	// These will be opened in the next update.
	private List<UIWindow> _windowsToOpen = new();

	public void AddWindow(UIWindow w) => _windowsToOpen.Add(w);
	public bool IsHoveringWindow => Children.Any(w => w.IsMouseHovering);

	public override void Update(GameTime t)
	{
		foreach (var w in _windowsToOpen) { Append(w); }
		_windowsToOpen.Clear();

		var toRemove = Elements.OfType<UIWindow>().Where(w => w.WantsClose).ToList();
		foreach (var w in toRemove)
		{
			w.WantsClose = false;
			RemoveChild(w);
		}

		UIWindow? focused = null;
		foreach (var w in Elements.OfType<UIWindow>())
		{
			if (w.WantsFocus)
			{
				w.WantsFocus = false;
				focused = w;
			}
		}

		// Bring focused window to the front.
		if (focused is not null)
		{
			RemoveChild(focused);
			Append(focused);
		}

		base.Update(t);
	}
}
