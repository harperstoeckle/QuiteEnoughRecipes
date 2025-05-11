using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria;

namespace QuiteEnoughRecipes;

public class Tooltips : GlobalItem
{
	public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
	{
		if (UISystem.IsOpen())
		{
			UISystem.Window.ModifyTooltips(Mod, item, tooltips);
		}
	}
}
