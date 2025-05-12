using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System;
using Terraria.UI;
using System.Linq;
using Terraria;

namespace QuiteEnoughRecipes;

/*
 * Maintains a set of windows and facilitates interaction between them. For example, it allows
 * windows to be opened and closed or moved to the top, and it allows existing windows to be
 * retrieved.
 */
public class UIWindowManager : UIState
{
	private Dictionary<string, UIWindow> _windows = new();

	/*
	 * In some cases (for example, `UIPopupWindow`, which closes itself during `DrawSelf`), a
	 * window will attempt to modify the child list while it is being iterated over. Rather than
	 * calling `RemoveChild` immediately, the call should be deferred until the start of the next
	 * frame by appending it to this list.
	 */
	private List<Action> _deferredCalls = new();

	public void AddWindow(string name, UIWindow w) => _windows.Add(name, w);

	public void Open(UIWindow w) => _deferredCalls.Add(() => { if (!HasChild(w)) { Append(w); } });
	public void Close(UIWindow w) => _deferredCalls.Add(() => RemoveChild(w));
	public bool IsOpen(UIWindow w) => HasChild(w);
	public bool IsHoveringWindow => Children.Any(w => w.IsMouseHovering);

	public override void LeftMouseDown(UIMouseEvent e)
	{
		base.LeftMouseDown(e);

		// Move the window to the top when it's clicked.
		if (e.Target is UIWindow w)
		{
			RemoveChild(w);
			Append(w);
		}
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		foreach (var call in _deferredCalls) { call(); }
		_deferredCalls.Clear();
		base.DrawSelf(sb);
	}
}
