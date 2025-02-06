using System.ComponentModel;
using Terraria.ModLoader.Config;
using Terraria.ModLoader;

namespace QuiteEnoughRecipes;

public class QERConfig : ModConfig
{
	public static QERConfig Instance => ModContent.GetInstance<QERConfig>();

	public override ConfigScope Mode => ConfigScope.ClientSide;

	[DefaultValue(false)]
	public bool ShouldPreloadItems;
}
