using Terraria.UI;

namespace QuiteEnoughRecipes;

/*
 * A container that may or may not contain another element. It is only active if it has an element
 * in it. This should be used in any situation where content is regularly swapped out, like tabs,
 * or with popup-like content, like option panels.
 */
public class UIContainer : UIElement
{
	public bool IsOpen => Elements.Count != 0;

	public UIContainer()
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

		OnOpen();
	}

	public void Close()
	{
		RemoveAllChildren();
		IgnoresMouseInteraction = true;
	}

	public virtual void OnOpen() {}

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
