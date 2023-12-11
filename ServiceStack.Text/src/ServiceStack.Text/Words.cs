// Copyright 2004-2011 Castle Project - http://www.castleproject.org/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace ServiceStack;

using System.Collections;
using System.Text.RegularExpressions;

/// <summary>
/// The Words class transforms words from one 
/// form to another. For example, from singular to plural.
/// </summary>
public static class Words
{
	private static readonly ArrayList plurals = new();
	private static readonly ArrayList singulars = new();
	private static readonly ArrayList uncountables = new();

	#region Default Rules

	static Words()
	{
		AddPlural("$", "s");
		AddPlural("s$", "s");
		AddPlural("(ax|test)is$", "$1es");
		AddPlural("(octop|vir)us$", "$1i");
		AddPlural("(alias|status)$", "$1es");
		AddPlural("(bu)s$", "$1ses");
		AddPlural("(buffal|tomat)o$", "$1oes");
		AddPlural("([ti])um$", "$1a");
		AddPlural("sis$", "ses");
		AddPlural("(?:([^f])fe|([lr])f)$", "$1$2ves");
		AddPlural("(hive)$", "$1s");
		AddPlural("([^aeiouy]|qu)y$", "$1ies");
		AddPlural("(x|ch|ss|sh)$", "$1es");
		AddPlural("(matr|vert|ind)ix|ex$", "$1ices");
		AddPlural("([m|l])ouse$", "$1ice");
		AddPlural("^(ox)$", "$1en");
		AddPlural("(quiz)$", "$1zes");

		AddSingular("s$", "");
		AddSingular("(n)ews$", "$1ews");
		AddSingular("([ti])a$", "$1um");
		AddSingular("((a)naly|(b)a|(d)iagno|(p)arenthe|(p)rogno|(s)ynop|(t)he)ses$", "$1$2sis");
		AddSingular("(^analy)ses$", "$1sis");
		AddSingular("([^f])ves$", "$1fe");
		AddSingular("(hive)s$", "$1");
		AddSingular("(tive)s$", "$1");
		AddSingular("([lr])ves$", "$1f");
		AddSingular("([^aeiouy]|qu)ies$", "$1y");
		AddSingular("(s)eries$", "$1eries");
		AddSingular("(m)ovies$", "$1ovie");
		AddSingular("(x|ch|ss|sh)es$", "$1");
		AddSingular("([m|l])ice$", "$1ouse");
		AddSingular("(bus)es$", "$1");
		AddSingular("(o)es$", "$1");
		AddSingular("(shoe)s$", "$1");
		AddSingular("(cris|ax|test)es$", "$1is");
		AddSingular("(octop|vir)i$", "$1us");
		AddSingular("(alias|status)es$", "$1");
		AddSingular("^(ox)en", "$1");
		AddSingular("(vert|ind)ices$", "$1ex");
		AddSingular("(matr)ices$", "$1ix");
		AddSingular("(quiz)zes$", "$1");

		AddIrregular("person", "people");
		AddIrregular("man", "men");
		AddIrregular("child", "children");
		AddIrregular("sex", "sexes");
		AddIrregular("move", "moves");

		AddUncountable("equipment");
		AddUncountable("information");
		AddUncountable("rice");
		AddUncountable("money");
		AddUncountable("species");
		AddUncountable("series");
		AddUncountable("fish");
		AddUncountable("sheep");
	}

	#endregion

	#region Rule inner class

	private class Rule
	{
		private readonly Regex regex;
		private readonly string replacement;

		public Rule(string pattern, string replacement)
		{
			regex = new Regex(pattern, RegexOptions.IgnoreCase);
			this.replacement = replacement;
		}

		public string Apply(string word)
		{
			if (!regex.IsMatch(word))
			{
				return null;
			}

			return regex.Replace(word, replacement);
		}
	}

	#endregion

	/// <summary>
	/// Return the plural of a word.
	/// </summary>
	/// <param name="word">The singular form</param>
	/// <returns>The plural form of <paramref name="word"/></returns>
	public static string Pluralize(string word)
	{
		return ApplyRules(plurals, word);
	}

	/// <summary>
	/// Return the singular of a word.
	/// </summary>
	/// <param name="word">The plural form</param>
	/// <returns>The singular form of <paramref name="word"/></returns>
	public static string Singularize(string word)
	{
		return ApplyRules(singulars, word);
	}

	/// <summary>
	/// Capitalizes a word.
	/// </summary>
	/// <param name="word">The word to be capitalized.</param>
	/// <returns><paramref name="word"/> capitalized.</returns>
	public static string Capitalize(string word)
	{
		return word.Substring(0, 1).ToUpper() + word.Substring(1).ToLower();
	}

	private static void AddIrregular(string singular, string plural)
	{
		AddPlural("(" + singular[0] + ")" + singular.Substring(1) + "$", "$1" + plural.Substring(1));
		AddSingular("(" + plural[0] + ")" + plural.Substring(1) + "$", "$1" + singular.Substring(1));
	}

	private static void AddUncountable(string word)
	{
		uncountables.Add(word.ToLower());
	}

	private static void AddPlural(string rule, string replacement)
	{
		plurals.Add(new Rule(rule, replacement));
	}

	private static void AddSingular(string rule, string replacement)
	{
		singulars.Add(new Rule(rule, replacement));
	}

	private static string ApplyRules(IList rules, string word)
	{
		string result = word;

		if (!uncountables.Contains(word.ToLower()))
		{
			for (int i = rules.Count - 1; i >= 0; i--)
			{
				Rule rule = (Rule)rules[i];

				if ((result = rule.Apply(word)) != null)
				{
					break;
				}
			}
		}

		return result;
	}
}