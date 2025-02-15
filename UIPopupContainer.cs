using Terraria.UI;

namespace QuiteEnoughRecipes;

/*
 * A container that may or may not contain another element. It is only active if it has an element
 * in it. This is used for things like the filter and sort panels, which are supposed to float above
 * other UI elements when open, but shouldn't do anything when closed.
 */
public class UIPopupContainer : UIElement
{
	public UIPopupContainer()
	{
		IgnoresMouseInteraction = true;
	}

	public void Open(UIElement e)
	{
		RemoveAllChildren();
		Append(e);
		e.Activate();
		e.Recalculate();
		IgnoresMouseInteraction = false;
	}

	public void Close()
	{
		RemoveAllChildren();
		IgnoresMouseInteraction = true;
	}

	// If `e` is the current active element, toggle it off. Otherwise, enable it.
	public void Toggle(UIElement e)
	{
		if (HasChild(e))
		{
			Close();
		}
		else
		{
			Open(e);
		}
	}
}
