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

		long val = DisplayedItem.GetStoreValue();
		(int Item, long Count)[] counts = CustomCurrencyManager.TryGetCurrencySystem(DisplayedItem.shopSpecialCurrency, out var customCurrency)
			? CustomCurrencySplit(customCurrency, val)
			: CoinCurrencySplit(val);

		// This is "Buy price: ".
		var priceText = $"{Lang.tip[50]}";
		if (val == 0)
		{
			// no value.
			priceText += $" {Lang.tip[51]}";
		}
		else
		{
			foreach (var (item, count) in counts)
			{
				if (count > 0)
				{
					priceText += $" {count}[i:{item}]";
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

	private static (int Item, long Count)[] CoinCurrencySplit(long price)
	{
		(int Item, long Count)[] counts = new int[4]{
			ItemID.PlatinumCoin,
			ItemID.GoldCoin,
			ItemID.SilverCoin,
			ItemID.CopperCoin
		}.Select(item => (item, 0L)).ToArray();

		for (int i = 3; i >= 1; --i)
		{
			counts[i].Count = price % 100;
			price /= 100;
		}

		counts[0].Count = price;

		return counts;
	}

	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_valuePerUnit")]
	extern static ref Dictionary<int, int> CustomCurrencySystem_valuePerUnit(CustomCurrencySystem self);

	/*
	 * Returns an array of (itemId, coin count) pairs representing how many of each item fulfil the price
	 * The array is ordered from greatest to least currency value
	 * Note: This uses a greedy approach and is not guaranteed to be the most optimal split of coins for non-canonical currencies
	 */
	private static (int Item, long Count)[] CustomCurrencySplit(CustomCurrencySystem currency, long price)
	{
		var currencyValues = CustomCurrencySystem_valuePerUnit(currency);

		(int Item, long Count)[] counts = currencyValues.Select(v => (v.Key, 0L)).ToArray();
		if (price == 0) { return counts; }

		int index = 0;
		foreach (var (item, worth) in currencyValues.OrderByDescending(v => v.Value))
		{
			var amount = price / worth;
			price %= amount;
			counts[index++].Count = amount;
		}

		// If this happens then the currency system is not canonical
		// so we add 1 to the smallest unit thus overpaying, but satisfying the cost
		if (price > 0)
		{
			counts[^1].Count++;
		}

		return counts;
	}
}
