using System;

namespace Leauge_Auto_Accept
{
    // Central place for the console color scheme
    internal static class Theme
    {
        public static readonly ConsoleColor Text = ConsoleColor.Gray;
        public static readonly ConsoleColor Dim = ConsoleColor.DarkGray;
        public static readonly ConsoleColor Accent = ConsoleColor.Cyan;
        public static readonly ConsoleColor Gold = ConsoleColor.Yellow;
        public static readonly ConsoleColor Good = ConsoleColor.Green;
        public static readonly ConsoleColor Bad = ConsoleColor.Red;

        // Logo fades from gold at the top to cyan at the bottom
        public static ConsoleColor LogoColor(int row, int rows)
        {
            if (rows <= 1)
            {
                return Gold;
            }
            double progress = (double)row / (rows - 1);
            if (progress < 0.34)
            {
                return Gold;
            }
            if (progress < 0.67)
            {
                return ConsoleColor.White;
            }
            return Accent;
        }

        // Picks a color for a setting's value text
        public static ConsoleColor ValueColor(string value)
        {
            string v = (value ?? "").Trim('.', ' ');

            if (v.Length == 0 || v == "Unselected" || v == "None")
            {
                return Dim;
            }
            if (v.StartsWith(Strings.Get("enabled")) || v == Strings.Get("yes"))
            {
                return Good;
            }
            if (v.StartsWith(Strings.Get("disabled")) || v == Strings.Get("no"))
            {
                return Bad;
            }
            if (double.TryParse(v, out _))
            {
                return Accent;
            }
            return Gold;
        }
    }
}
