using Terraria.ModLoader;
using Terraria.UI;

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
			grid.Append(new UIItemPanel(entry.Item));
		}

		Append(grid);
	}
}
