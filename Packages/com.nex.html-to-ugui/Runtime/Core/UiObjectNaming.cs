using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace HtmlToUGUI
{
    public static class UiObjectNaming
    {
        public static string Prefix(string component)
        {
            switch (component.ToLowerInvariant())
            {
                case "text": return "Txt";
                case "image": return "Img";
                case "rawimage": return "RawImg";
                case "button": return "Btn";
                case "inputfield": return "Input";
                case "scrollview": return "Scroll";
                case "toggle": return "Toggle";
                case "slider": return "Slider";
                default: return "Group";
            }
        }

        public static string Pascal(string value)
        {
            var result = "";
            foreach (Match word in Regex.Matches(value ?? "", @"[A-Za-z0-9]+"))
                result += char.ToUpperInvariant(word.Value[0]) + word.Value.Substring(1);
            if (result.Length == 0) return "Element";
            return char.IsDigit(result[0]) ? "Element" + result : result;
        }

        public static string UniqueUnder(Transform parent, string name, bool numberedItem = false)
        {
            var used = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            if (parent != null) foreach (Transform child in parent) used.Add(child.name);
            if (!numberedItem && !used.Contains(name)) return name;
            var index = numberedItem ? 1 : 2;
            string candidate;
            do { candidate = name + "_" + (index++).ToString("D2", System.Globalization.CultureInfo.InvariantCulture); }
            while (used.Contains(candidate));
            return candidate;
        }
    }
}
