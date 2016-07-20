//
//  Language.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;

namespace NScumm.Core
{
	/// <summary>
	/// List of Languages.
	/// </summary>
	public enum Language
	{
		ZH_CNA,
		ZH_TWN,
		HR_HRV,
		CZ_CZE,
		NL_NLD,
		EN_ANY,
		// Generic English (when only one game version exist)
		EN_GRB,
		EN_USA,
		FR_FRA,
		DE_DEU,
		GR_GRE,
		HE_ISR,
		HU_HUN,
		IT_ITA,
		JA_JPN,
		KO_KOR,
		LV_LAT,
		NB_NOR,
		PL_POL,
		PT_BRA,
		RU_RUS,
		ES_ESP,
		SE_SWE,

		UNK_LANG = -1
		// Use default language (i.e. none specified)
	}

	class LanguageDescription
	{
		public string code;
		public string unixLocale;
		public string description;
		public Language id;

		public LanguageDescription (string code, string unixLocale, string description, Language id)
		{
			this.code = code;
			this.unixLocale = unixLocale;
			this.description = description;
			this.id = id;
		}
	}

	public static class LanguageHelper
	{
		private static readonly LanguageDescription[] g_languages = new LanguageDescription[] {
			new LanguageDescription ("zh-cn", "zh_CN", "Chinese (China)", Language.ZH_CNA),
			new LanguageDescription ("zh", "zh_TW", "Chinese (Taiwan)", Language.ZH_TWN),
			new LanguageDescription ("hr", "hr_HR", "Croatian", Language.HR_HRV),
			new LanguageDescription ("cz", "cs_CZ", "Czech", Language.CZ_CZE),
			new LanguageDescription ("nl", "nl_NL", "Dutch", Language.NL_NLD),
			new LanguageDescription ("en", "en", "English", Language.EN_ANY), // Generic English (when only one game version exist)
			new LanguageDescription ("gb", "en_GB", "English (GB)", Language.EN_GRB),
			new LanguageDescription ("us", "en_US", "English (US)", Language.EN_USA),
			new LanguageDescription ("fr", "fr_FR", "French", Language.FR_FRA),
			new LanguageDescription ("de", "de_DE", "German", Language.DE_DEU),
			new LanguageDescription ("gr", "el_GR", "Greek", Language.GR_GRE),
			new LanguageDescription ("he", "he_IL", "Hebrew", Language.HE_ISR),
			new LanguageDescription ("hb", "he_IL", "Hebrew", Language.HE_ISR), // Deprecated
			new LanguageDescription ("hu", "hu_HU", "Hungarian", Language.HU_HUN),
			new LanguageDescription ("it", "it_IT", "Italian", Language.IT_ITA),
			new LanguageDescription ("jp", "ja_JP", "Japanese", Language.JA_JPN),
			new LanguageDescription ("kr", "ko_KR", "Korean", Language.KO_KOR),
			new LanguageDescription ("lv", "lv_LV", "Latvian", Language.LV_LAT),
			new LanguageDescription ("nb", "nb_NO", "Norwegian Bokm\xE5l", Language.NB_NOR), // TODO Someone should verify the unix locale
			new LanguageDescription ("pl", "pl_PL", "Polish", Language.PL_POL),
			new LanguageDescription ("br", "pt_BR", "Portuguese", Language.PT_BRA),
			new LanguageDescription ("ru", "ru_RU", "Russian", Language.RU_RUS),
			new LanguageDescription ("es", "es_ES", "Spanish", Language.ES_ESP),
			new LanguageDescription ("se", "sv_SE", "Swedish", Language.SE_SWE),
			new LanguageDescription (null, null, null, Language.UNK_LANG)
		};

		public static Language ParseLanguage (string str)
		{
			if (string.IsNullOrEmpty (str))
				return Language.UNK_LANG;

			foreach (var l in g_languages) {
				if (string.Equals (str, l.code, StringComparison.OrdinalIgnoreCase))
					return l.id;
			}

			return Language.UNK_LANG;
		}
	}
}
