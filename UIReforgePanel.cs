using Microsoft.Xna.Framework;
using System.Text;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;

namespace QuiteEnoughRecipes;
public class UIReforgePanel : UIAutoExtend
{
	public UIReforgePanel(Item prefixedItem)
	{
		Height.Pixels = 50;
		Width.Percent = 1;

		Append(new UIItemPanel(prefixedItem));

		var costText = new UIText($"Cost: {GetValueText(GetReforgePrice(prefixedItem))}", 0.8f);
		costText.Left.Pixels = 60;
		costText.Top.Pixels = 3;
		Append(costText);

		var reforgeIcon = new UIImage(TextureAssets.Reforge[0]);
		reforgeIcon.Left.Pixels = 60;
		reforgeIcon.Top.Pixels = 20;
		Append(reforgeIcon);

		var prefixText = new UIText(Lang.prefix[prefixedItem.prefix]);
		prefixText.Left.Pixels = 100;
		prefixText.Top.Pixels = 25;
		Append(prefixText);
	}

	/*
	 * Calculates the reforge price based on a given item
	 * Taken from Main.DrawInventory()
	 * Note: NPC happiness and the discount calculations are omitted 
	 * due to potential side effects
	 */
	private static long GetReforgePrice(Item item)
	{
		int price = item.value;
		price *= item.stack; // Added by TML should always be 1 in this case

		bool canApplyDiscount = false;
		if (ItemLoader.ReforgePrice(item, ref price, ref canApplyDiscount))
		{
			price /= 3;
		}
		return price;
	}

	/*
	 * Creates colored coin text based on a given value as seen when reforging  
	 * Taken from Main.DrawInventory()
	 */
	private static string GetValueText(long value)
	{
		var text = new StringBuilder();
		var coins = Utils.CoinsSplit(value);
		var copper = coins[0];
		var silver = coins[1];
		var gold = coins[2];
		var platinum = coins[3];

		void AddCoinText(Color color, int amount, int langIndex) =>
			text.Append($"[c/{color.Hex3()}:{amount} {Lang.inter[langIndex].Value}] ");

		if (platinum > 0)
			AddCoinText(Colors.AlphaDarken(Colors.CoinPlatinum), platinum, 15);

		if (gold > 0)
			AddCoinText(Colors.AlphaDarken(Colors.CoinGold), gold, 16);

		if (silver > 0)
			AddCoinText(Colors.AlphaDarken(Colors.CoinSilver), silver, 17);

		if (copper > 0)
			AddCoinText(Colors.AlphaDarken(Colors.CoinCopper), copper, 18);

		return text.ToString();
	}
}
