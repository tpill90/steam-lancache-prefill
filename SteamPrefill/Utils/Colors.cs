using Spectre.Console;

namespace SteamPrefill.Utils
{
    public static class SpectreColors
    {
        public static string Blue(object inputObj) => $"[blue]{inputObj}[/]";
        public static string Cyan(object inputObj) => $"[rgb(97,214,214)]{inputObj}[/]";
        public static string LightBlue(object inputObj) => $"[deepskyblue3]{inputObj}[/]";
        public static string LightYellow(object inputObj) => $"[rgb(249,241,165)]{inputObj}[/]";
        public static string Green(object inputObj) => $"[green]{inputObj}[/]";
        public static string Grey(object inputObj) => $"[grey]{inputObj}[/]";
        public static string Magenta(object inputObj) => $"[rgb(217,85,217)]{inputObj}[/]";
        public static string Olive(object inputObj) => $"[yellow4_1]{inputObj}[/]";
        public static string Red(object inputObj) => $"[red]{inputObj}[/]";
        public static string White(object inputObj) => $"[white]{inputObj}[/]";
        public static string Yellow(object inputObj) => $"[gold1]{inputObj}[/]";

        // Text styles
        public static string Underline(object inputObj) => $"[underline]{inputObj}[/]";

        public static Color MediumPurple1 = Color.MediumPurple1;
    }
}