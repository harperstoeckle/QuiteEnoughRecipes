using System.Collections.Generic;
using System.Linq;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria;

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

		var coinIds = new int[4]{
			ItemID.PlatinumCoin,
			ItemID.GoldCoin,
			ItemID.SilverCoin,
			ItemID.CopperCoin
		};
		var coinValues = new int[4];

		int val = DisplayedItem.value;
		for (int i = 3; i >= 1; --i)
		{
			coinValues[i] = val % 100;
			val /= 100;
		}

		coinValues[0] = val;

		// This is "Buy price: ".
		var priceText = $"{Lang.tip[50]}";
		if (DisplayedItem.value == 0)
		{
			// no value.
			priceText += $" {Lang.tip[51]}";
		}
		else
		{
			for (int i = 0; i < 4; ++i)
			{
				if (coinValues[i] > 0)
				{
					priceText += $" {coinValues[i]}[i:{coinIds[i]}]";
				}
			}
		}

		tooltips.Add(new(mod, "QER: buy price", priceText){
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
