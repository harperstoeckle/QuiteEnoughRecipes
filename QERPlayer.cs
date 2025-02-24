using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria;

namespace QuiteEnoughRecipes;

public class QERPlayer : ModPlayer
{
	public static bool ShouldGoBackInHistory { get; private set; } = false;
	public static bool ShouldGoForwardInHistory { get; private set; } = false;

	public override void ProcessTriggers(TriggersSet ts)
	{
		ShouldGoBackInHistory = false;
		ShouldGoForwardInHistory = false;
		if (UISystem.BackKey?.JustPressed ?? false)
		{
			ShouldGoForwardInHistory = Main.keyState.PressingShift();
			ShouldGoBackInHistory = !ShouldGoForwardInHistory;
		}

		if (UISystem.UI == null) { return; }

		if (UISystem.OpenUIKey?.JustPressed ?? false)
		{
			if (UISystem.UI.IsOpen())
			{
				UISystem.UI.Close();
			}
			else
			{
				UISystem.UI.Open();
			}
		}

		if ((UISystem.HoverSourcesKey?.JustPressed ?? false) && Main.HoverItem != null && !Main.HoverItem.IsAir)
		{
			UISystem.UI.ShowSources(new ItemIngredient(Main.HoverItem));
			UISystem.UI.Open();
		}

		if ((UISystem.HoverUsesKey?.JustPressed ?? false) && Main.HoverItem != null && !Main.HoverItem.IsAir)
		{
			UISystem.UI.ShowUses(new ItemIngredient(Main.HoverItem));
			UISystem.UI.Open();
		}
	}
}
