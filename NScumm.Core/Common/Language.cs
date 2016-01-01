namespace NScumm.Core.Common
{
    /// <summary>
    /// List of game language.
    /// </summary>
    public enum Language
    {
        ZH_CNA,
        ZH_TWN,
        HR_HRV,
        CZ_CZE,
        NL_NLD,
        EN_ANY,     // Generic English (when only one game version exist)
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

        UNK_LANG = -1   // Use default language (i.e. none specified)
    }
}
