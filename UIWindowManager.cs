using System.Collections.Generic;
using Terraria.UI;

namespace QuiteEnoughRecipes;

/*
 * Maintains a set of windows and facilitates interaction between them. For example, it allows
 * windows to be opened and closed or moved to the top, and it allows existing windows to be
 * retrieved.
 */
public class UIWindowManager : UIState
{
	private Dictionary<string, UIWindow> _windows = new();

	public void AddWindow(string name, UIWindow w) => _windows.Add(name, w);

	public void Open(UIWindow w)
	{
		if (!HasChild(w)) { Append(w); }
	}

	public void Close(UIWindow w) => RemoveChild(w);
	public bool IsOpen(UIWindow w) => HasChild(w);

	public override void LeftMouseDown(UIMouseEvent e)
	{
		// Move the window to the top when it's clicked.
		if (e.Target is UIWindow w)
		{
			RemoveChild(w);
			Append(w);
		}
	}
}
