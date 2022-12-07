namespace LancachePrefill.Common
{
    /// <summary>
    /// https://spectreconsole.net/appendix/colors
    /// https://htmlcolorcodes.com/color-picker/
    /// </summary>
    public static class SpectreFormatters
    {
        public static string Blue(object inputObj) => $"[blue]{inputObj}[/]";
        public static string Cyan(object inputObj) => $"[rgb(97,200,214)]{inputObj}[/]";

        public static string LightBlue(object inputObj) => $"[deepskyblue3]{inputObj}[/]";
        public static string LightGreen(object inputObj) => $"[{SpectreColors.LightGreen.ToMarkup()}]{inputObj}[/]";
        public static string LightRed(object inputObj) => $"[{SpectreColors.LightRed.ToMarkup()}]{inputObj}[/]";
        public static string LightYellow(object inputObj) => $"[rgb(249,241,165)]{inputObj}[/]";

        public static string ForestGreen(object inputObj) => $"[rgb(121,158,105)]{inputObj}[/]";

        public static string Green(object inputObj) => $"[green]{inputObj}[/]";
        public static string Grey(object inputObj) => $"[grey]{inputObj}[/]";

        public static string Magenta(object inputObj) => $"[rgb(200,85,217)]{inputObj}[/]";
        public static string MediumPurple(object inputObj) => $"[{SpectreColors.MediumPurple1.ToMarkup()}]{inputObj}[/]";

        public static string Olive(object inputObj) => $"[yellow4_1]{inputObj}[/]";
        public static string Red(object inputObj) => $"[red]{inputObj}[/]";
        public static string White(object inputObj) => $"[white]{inputObj}[/]";

        // Text styles
        public static string Bold(object inputObj) => $"[bold]{inputObj}[/]";
        public static string Underline(object inputObj) => $"[underline]{inputObj}[/]";
    }

    public static class SpectreColors
    {
        public static Color LightGreen => new Color(99, 201, 101);
        public static Color LightRed => new Color(255, 110, 110);

        public static Color MediumPurple1 => Color.MediumPurple1;
    }
}