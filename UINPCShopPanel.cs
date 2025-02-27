using System.Collections.Generic;
using System.Linq;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria;
using Terraria.GameContent.UI;
using System.Runtime.CompilerServices;

namespace QuiteEnoughRecipes;

// Displays an NPC along with the items they sell.
public class UINPCShopPanel : UIAutoExtend
{
	public UINPCShopPanel(AbstractNPCShop shop)
	{
		Width.Percent = 1;

		Append(new UINPCPanel(shop.NpcType){
			Width = new StyleDimension(82, 0),
			Height = new StyleDimension(82, 0)
		});

		var grid = new UIAutoExtendGrid(){
			Width = new StyleDimension(-92, 1),
			HAlign = 1
		};

		foreach (var entry in shop.ActiveEntries)
		{
			grid.Append(new UIShopItemPanel(entry));
		}

		Append(grid);
	}
}

// Shows the price at the bottom.
file class UIShopItemPanel : UIItemPanel
{
	string? _conditions = null;

	public UIShopItemPanel(AbstractNPCShop.Entry entry) : base(entry.Item)
	{
		var conditionDescs = entry.Conditions
			.Select(c => c.Description.Value)
			.Where(d => !string.IsNullOrWhiteSpace(d));
		_conditions = string.Join("\n", conditionDescs);
	}

	public override void ModifyTooltips(Mod mod, List<TooltipLine> tooltips)
	{
		base.ModifyTooltips(mod, tooltips);

		if (DisplayedItem == null) { return; }

		var buyPriceText = QuiteEnoughRecipes.GetBuyPriceText(DisplayedItem);
		tooltips.Add(new(mod, "QER: buy price", buyPriceText){
			OverrideColor = Main.OurFavoriteColor
		});

		if (!string.IsNullOrWhiteSpace(_conditions))
		{
			tooltips.Add(new(mod, "QER: buy conditions", _conditions){
				OverrideColor = Main.OurFavoriteColor
			});
		}
	}
}
