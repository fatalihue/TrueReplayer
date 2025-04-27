using System;
using System.Collections.Generic;

namespace TrueReplayer.Models
{
    public class ActionItem
    {
        public string ActionType { get; set; }
        public string Key { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Delay { get; set; }
        public string Comment { get; set; }

        public string DisplayKey
        {
            get
            {
                if (string.IsNullOrEmpty(Key)) return "";

                // Números D0–D9 → 0–9
                if (Key.StartsWith("D") && Key.Length == 2 && char.IsDigit(Key[1]))
                    return Key[1].ToString();

                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    {"162", "Ctrl"}, {"163", "Ctrl"},
                    {"160", "Shift"}, {"161", "Shift"},
                    {"20", "Caps Lock"}, {"144", "Num Lock"}, {"145", "Scroll Lock"},
                    {"91", "Win"}, {"92", "Win"},
                    {"164", "Alt"}, {"165", "Alt"}, {"Menu", "Alt"},
                    {"Oem1", ";"}, {"Oem2", "/"}, {"Oem3", "`"},
                    {"Oem4", "["}, {"Oem5", "\\"}, {"Oem6", "]"}, {"Oem7", "'"},
                    {"OemComma", ","}, {"OemPeriod", "."},
                    {"OemMinus", "-"},
                    {"OemPlus", "="}
                };

                return map.TryGetValue(Key, out var readable) ? readable : Key;
            }
        }
    }
}
