using System;
using UnityEngine;

namespace ProbablyMCM.Core
{
    public static class McmLoc
    {
        public const string MOD_ID = "probably_mcm";

        public static string Get(string key, string fallback, params object[] args)
        {
            try
            {
                string localized = ModHelper.GetLocalized(MOD_ID, key, args);
                if (string.IsNullOrEmpty(localized) || localized == key)
                {
                    return args != null && args.Length > 0 ? string.Format(fallback, args) : fallback;
                }
                return localized;
            }
            catch
            {
                return args != null && args.Length > 0 ? string.Format(fallback, args) : fallback;
            }
        }
    }
}
