using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria;

namespace QuiteEnoughRecipes;

public class Tooltips : GlobalItem
{
	public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
	{
		if (Main.InGameUI.CurrentState is UIQERState s)
		{
			s.ModifyTooltips(Mod, tooltips);
		}
	}
}
