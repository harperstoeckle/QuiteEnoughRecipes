using Microsoft.Xna.Framework;
using System;
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
	/*
	 * These will be opened in the next update. Elements in here are guaranteed to be `UIElement`s
	 * as well as `IWindowManagerElement`s, but there's no clear way to enforce this.
	 */
	private List<IWindowManagerElement> _toOpen = new();
	private IWindowManagerElement? _dragging = null;

	public void Open<T>(T w) where T : UIElement, IWindowManagerElement => _toOpen.Add(w);

	public bool IsHoveringWindow => Children.Any(w => w.IsMouseHovering);

	public override void Update(GameTime t)
	{
		foreach (var w in _toOpen)
		{
			if (!HasChild(w as UIElement))
			{
				Append(w as UIElement);
				w.OnOpen();
			}
		}
		_toOpen.Clear();

		var toRemove = Elements.OfType<IWindowManagerElement>().Where(w => w.WantsClose).ToList();
		foreach (var w in toRemove)
		{
			w.WantsMoveToFront = false;

			w.WantsDrag = DragRequestState.None;
			if (_dragging == w) { w.StopDragging(); }

			w.WantsClose = false;
			w.OnClose();
			RemoveChild(w as UIElement);
		}

		// Sort elements that want to move to the front, to the front.
		Func<UIElement, int> moveToFrontSortKey = e => {
			return e is IWindowManagerElement w && w.WantsMoveToFront ? 1 : 0;
		};
		Elements.Sort((a, b) => moveToFrontSortKey(a).CompareTo(moveToFrontSortKey(b)));

		IWindowManagerElement? newDragging = _dragging;
		foreach (var w in Elements.OfType<IWindowManagerElement>())
		{
			w.WantsMoveToFront = false;

			if (w.WantsDrag == DragRequestState.Stop && _dragging == w)
			{
				newDragging = null;
			}
			else if (w.WantsDrag == DragRequestState.Start)
			{
				newDragging = w;
			}

			w.WantsDrag = DragRequestState.None;
		}

		if (newDragging != _dragging)
		{
			_dragging?.OnStopDragging();
			_dragging = newDragging;
			_dragging?.OnStartDragging();
		}

		// The dragged element should always be at the very top.
		if (_dragging is not null && Elements.LastOrDefault() != _dragging)
		{
			RemoveChild(_dragging as UIElement);
			Append(_dragging as UIElement);
		}

		base.Update(t);
	}

	public override void LeftMouseUp(UIMouseEvent e)
	{
		base.LeftMouseUp(e);

		foreach (var w in Elements.OfType<IWindowManagerElement>())
		{
			w.OnWindowManagerLeftMouseUp(e);
		}
	}

	public override void RightMouseUp(UIMouseEvent e)
	{
		base.RightMouseUp(e);

		foreach (var w in Elements.OfType<IWindowManagerElement>())
		{
			w.OnWindowManagerRightMouseUp(e);
		}
	}

	public override void MiddleMouseUp(UIMouseEvent e)
	{
		base.MiddleMouseUp(e);

		foreach (var w in Elements.OfType<IWindowManagerElement>())
		{
			w.OnWindowManagerMiddleMouseUp(e);
		}
	}
}

public enum DragRequestState
{
	None,
	Start,
	Stop,
}

/*
 * Element that the window manager can manage. Something implementing this should always also
 * derive from `UIElement, but I can't figure out how to enforce that.
 */
public interface IWindowManagerElement
{
	public bool WantsMoveToFront { get; set; }
	public bool WantsClose { get; set; }
	public DragRequestState WantsDrag { get; set; }

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
	public static void Close(this IWindowManagerElement e) => e.WantsClose = true;
	public static void StartDragging(this IWindowManagerElement e)
	{
		e.WantsDrag = DragRequestState.Start;
	}
	public static void StopDragging(this IWindowManagerElement e)
	{
		e.WantsDrag = DragRequestState.Stop;
	}
	public static void MoveToFront(this IWindowManagerElement e)
	{
		e.WantsMoveToFront = true;
	}
}
