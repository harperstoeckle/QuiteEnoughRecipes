using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria;

namespace QuiteEnoughRecipes;

public class QERPlayer : ModPlayer
{
	public static bool BackRequested { get; private set; } = false;

	public override void ProcessTriggers(TriggersSet ts)
	{
		BackRequested = UISystem.BackKey?.JustPressed ?? false;

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
