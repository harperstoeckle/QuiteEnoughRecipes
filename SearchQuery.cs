using System.Linq;
using System.Text.RegularExpressions;

namespace QuiteEnoughRecipes;

// Used to search ingredients.
public class SearchQuery
{
	private string _name;
	private string? _mod;
	private string? _tooltip;

	public bool Matches(IIngredient i)
	{
		if (!string.IsNullOrWhiteSpace(_name) &&
			(i.Name == null || !NormalizeForSearch(i.Name).Contains(_name)))
		{
			return false;
		}

		if (!string.IsNullOrWhiteSpace(_mod) &&
			(i.Mod == null || !NormalizeForSearch(RemoveWhitespace(i.Mod.DisplayNameClean)).Contains(_mod)))
		{
			return false;
		}

		if (!string.IsNullOrWhiteSpace(_tooltip))
		{
			// Needed to be usable in a lambda.
			var tooltip = _tooltip;
			if (!i.GetTooltipLines().Any(l => NormalizeForSearch(l).Contains(tooltip)))
			{
				return false;
			}
		}

		return true;
	}

	public static SearchQuery FromSearchText(string text)
	{
		var query = new SearchQuery();
		var parts = text.Split("#", 2);

		if (parts.Length >= 2)
		{
			query._tooltip = NormalizeForSearch(parts[1]);
		}

		/*
		 * TODO: These could be made more efficient by not re-compiling the regexes every time, but
		 * it's probably fine since this only happens once when the search is changed.
		 *
		 * This will just use the first specified mod and ignore the rest.
		 */
		var modCaptures = Regex.Matches(parts[0], @"@(\S+)");
		if (modCaptures.Count >= 1)
		{
			query._mod = NormalizeForSearch(modCaptures[0].Groups[1].Value);
		}

		/*
		 * We remove *any* @ from the name, even if it doesn't have any characters after it that
		 * might be part of a mod.
		 */
		query._name = NormalizeForSearch(Regex.Replace(parts[0], @"@\S*\s*", ""));

		return query;
	}

	// Used to remove whitespace
	private static readonly Regex WhitespaceRegex = new(@"\s+");
	private string RemoveWhitespace(string s)
	{
		return WhitespaceRegex.Replace(s, "");
	}

	/*
	 * Put text in a standard form so that searching can be effectively done using
	 * `string.Contains`.
	 */
	private static string NormalizeForSearch(string s)
	{
		return s.Trim().ToLower();
	}
}
