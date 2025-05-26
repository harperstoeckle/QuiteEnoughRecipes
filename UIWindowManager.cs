using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace QuiteEnoughRecipes;

/*
 * Maintains a set of UI elements that I will, from here on out, be referring to as "windows". The
 * window manager does not handle the position nor appearance of windows, but it does handle their
 * stacking order and removal.
 *
 * The window manager also manages drag-and-drop behavior. At any time, a window can indicate that
 * it is starting to be dragged (regardless of what it's actually doing) and the window manager
 * will keep track of that.
 */
public class UIWindowManager : UIState
{
	/*
	 * These will be opened in the next update. Elements in here are guaranteed to be `UIElement`s
	 * as well as `IWindowManagerElement`s, but there's no clear way to enforce this.
	 */
	private List<IWindow> _toOpen = new();
	private List<Action> _deferredCalls = new();

	/*
	 * When an item panel is being hovered, this keeps track of it. This is needed so that we can
	 * have the panel do tooltip modifications.
	 */
	private UIItemPanel? _hoveredItemPanel = null;

	/*
	 * `Dragging` is the element currently being dragged, and `JustDropped` is the element just
	 * dropped in the current tick.
	 */
	public IWindow? Dragging { get; private set; } = null;
	public IWindow? JustDropped { get; private set; } = null;

	/*
	 * Dragged windows are usually set to `IgnoresMouseInteraction` so they can interact with other
	 * elements, so we need to also account for that when deciding whether to block inventory
	 * interactions.
	 */
	public bool ShouldBlockInventoryInteraction =>
		Children.Any(w => w.IsMouseHovering) || Dragging is not null;

	public void Open<T>(T w) where T : UIElement, IWindow => _toOpen.Add(w);

	/*
	 * Defer a function to be called in the next update, just after windows have been rearranged but
	 * before children are updated.
	 *
	 * For example, let's say a UI element `e` wants to close a window `w` in the window manager and
	 * add it as a child of itself with different dimensions. Then it should
	 *
	 * 1. Tell the window to reparent to `e`.
	 * 2. Defer a call that modifies `e`'s dimensions.
	 *
	 * If the dimensions are modified *before* this, then there may be a short frame where the
	 * window is drawn in the manager with the wrong dimensions.
	 */
	public void DeferCall(Action proc) => _deferredCalls.Add(proc);

	public override void Update(GameTime t)
	{
		JustDropped = null;

		foreach (var w in _toOpen)
		{
			if (!HasChild(w as UIElement))
			{
				Append(w as UIElement);
				w.OnOpen();
			}
		}
		_toOpen.Clear();

		IWindow? newDragging = Dragging;
		foreach (var w in Elements.OfType<IWindow>())
		{
			w.WindowState.WantsMoveToFront = false;

			// Closed elements also stop getting dragged.
			if ((w.WindowState.WantsClose || w.WindowState.WantsDrag == DragRequestState.Stop) && Dragging == w)
			{
				newDragging = null;
			}
			else if (w.WindowState.WantsDrag == DragRequestState.Start)
			{
				newDragging = w;
			}

			w.WindowState.WantsDrag = DragRequestState.None;
		}

		if (newDragging != Dragging)
		{
			JustDropped = Dragging;
			Dragging?.OnStopDragging();
			Dragging = newDragging;
			Dragging?.OnStartDragging();
		}

		var toRemove = Elements.OfType<IWindow>()
			.Where(w => w.WindowState.WantsClose || w.WindowState.ReparentDestination is not null)
			.ToList();
		foreach (var w in toRemove)
		{
			if (w.WindowState.WantsClose)
			{
				w.WindowState.WantsClose = false;
				w.OnClose();
				RemoveChild(w as UIElement);
			}

			if (w.WindowState.ReparentDestination is UIElement dest)
			{
				w.WindowState.ReparentDestination = null;
				dest.Append(w as UIElement);
			}
		}

		/*
		 * This *must* be done with `OrderBy` instead of `Sort`, because `OrderBy` is stable, but
		 * `Sort` is not.
		 */
		var sortedElements = Elements.OrderBy(
				e => {
					if (e is IWindow w)
					{
						/*
						 * The dragged element is on top, followed by every other element in order
						 * of z order.
						 */
						return (w == Dragging ? 1 : 0, w.WindowState.ZOrder, w.WindowState.WantsMoveToFront ? 1 : 0);
					}
					else
					{
						return (0, 0, 0);
					}
				}).ToList();
		Elements.Clear();
		Elements.AddRange(sortedElements);

		foreach (var proc in _deferredCalls) { proc(); }
		_deferredCalls.Clear();

		base.Update(t);
	}

	public override void LeftMouseUp(UIMouseEvent e)
	{
		base.LeftMouseUp(e);

		foreach (var w in Elements.OfType<IWindow>())
		{
			w.OnWindowManagerLeftMouseUp(e);
		}
	}

	public override void RightMouseUp(UIMouseEvent e)
	{
		base.RightMouseUp(e);

		foreach (var w in Elements.OfType<IWindow>())
		{
			w.OnWindowManagerRightMouseUp(e);
		}
	}

	public override void MiddleMouseUp(UIMouseEvent e)
	{
		base.MiddleMouseUp(e);

		foreach (var w in Elements.OfType<IWindow>())
		{
			w.OnWindowManagerMiddleMouseUp(e);
		}
	}

	/*
	 * TODO: Handling for modifying tooltips should not be here, but the window manager element is
	 * a parent to all of the QER UI stuff, so this is the only place this code can go when using
	 * the current method (catching mouse events that have bubbled up).
	 */
	public override void LeftClick(UIMouseEvent e)
	{
		base.LeftClick(e);

		if (!(e.Target is UIQERSearchBar))
		{
			UIQERSearchBar.UnfocusAll();
		}

		if (e.Target is IIngredientElement s && s.Ingredient != null)
		{
			UISystem.ShowSources(s.Ingredient);
		}
	}

	public override void RightClick(UIMouseEvent e)
	{
		base.RightClick(e);

		if (!(e.Target is UIQERSearchBar))
		{
			UIQERSearchBar.UnfocusAll();
		}

		if (e.Target is IIngredientElement s && s.Ingredient != null)
		{
			UISystem.ShowUses(s.Ingredient);
		}
	}

	public override void MouseOver(UIMouseEvent e)
	{
		base.MouseOver(e);
		if (e.Target is UIItemPanel p) { _hoveredItemPanel = p; }
	}

	public override void MouseOut(UIMouseEvent e)
	{
		base.MouseOut(e);
		if (e.Target == _hoveredItemPanel) { _hoveredItemPanel = null; }
	}

	public void ModifyTooltips(Mod mod, Item item, List<TooltipLine> tooltips)
	{
		// Prevent weird situations where the wrong tooltip can be modified.
		if ((_hoveredItemPanel?.DisplayedItem?.type ?? 0) != item.type) { return; }
		_hoveredItemPanel?.ModifyTooltips(mod, tooltips);
	}
}

public enum DragRequestState
{
	None,
	Start,
	Stop,
}

/*
 * Keeps track of data needed used to inform the window manager of what should happen with this
 * window.
 */
public class WindowState
{
	public bool WantsMoveToFront = false;
	public bool WantsClose = false;
	public DragRequestState WantsDrag = DragRequestState.None;
	public UIElement? ReparentDestination = null;
	public int ZOrder = 0;
}

/*
 * Something implementing `IWindow` should also be derived from `UIElement` to be usable with a
 * window manager. A "window" can be basically any UI element; it doesn't *have* to be a
 * traditional floating window.
 */
public interface IWindow
{
	public WindowState WindowState { get; }

	/*
	 * In some cases, it's impossible to directly tie dragging with left clicking an element. For
	 * example, consider an ingredient slot that, when clicked, creates an ingredient icon that the
	 * user can drag to another slot. Since the "draggable element" did not exist when the slot was
	 * clicked, it's impossible for it to have been clicked to start dragging. This should instead
	 * be handled as follows:
	 *
	 * 1. The slot creates the draggable element `e`, and adds it to the window manager with
	 *    `WindowManager.Open(e)`
	 * 2. The slot calls `e.StartDragging()` to inform the window manager that it will be dragged.
	 * 3. On the next update, the window manager will call `e.OnStartDragging`, which is where the
	 *    draggable element should implement its code to start dragging.
	 * 4. When the user releases the mouse, the manager will automatically call `OnStopDragging` on
	 *    `e`.
	 */
	public void OnStartDragging() {}
	public void OnStopDragging() {}

	/*
	 * `OnOpen` is called *after* the element is appended to the window manager, and `OnClose` is
	 * called *before* it is removed.
	 */
	public void OnOpen() {}
	public void OnClose() {}

	/*
	 * Unlike the `UIElement` events, these are called *any* time these events happen anywhere in
	 * the window manager. This is useful, for example, when a dragged element (as explained above)
	 * might want to react to being released despite not technically being the last element clicked.
	 */
	public void OnWindowManagerLeftMouseUp(UIMouseEvent e) {}
	public void OnWindowManagerRightMouseUp(UIMouseEvent e) {}
	public void OnWindowManagerMiddleMouseUp(UIMouseEvent e) {}
}

/*
 * For convenience. I wish I could implement these as sealed methods with default implementations
 * directly in `IWindowManagerElement`, but those require an explicit cast to be called inside the
 * implementing class.
 */
public static class WindowManagerElementExtensions
{
	public static void Close(this IWindow e) => e.WindowState.WantsClose = true;
	public static void StartDragging(this IWindow e)
	{
		e.WindowState.WantsDrag = DragRequestState.Start;
	}
	public static void StopDragging(this IWindow e)
	{
		e.WindowState.WantsDrag = DragRequestState.Stop;
	}
	public static void MoveToFront(this IWindow e)
	{
		e.WindowState.WantsMoveToFront = true;
	}
	public static void ReparentTo(this IWindow e, UIElement newParent)
	{
		e.WindowState.ReparentDestination = newParent;
	}
}
