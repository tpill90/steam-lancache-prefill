namespace SteamPrefill.CliCommands
{
    public sealed class NullableBoolConverter : BindingConverter<bool?>
    {
        // Required in order to prevent CliFx from showing the unnecessary 'Default: "False"' text for boolean flags
        public override bool? Convert(string rawValue)
        {
            return true;
        }
    }

    public sealed class OperatingSystemConverter : BindingConverter<OperatingSystem>
    {
        public override OperatingSystem Convert(string rawValue)
        {
            if (rawValue == null)
            {
                AnsiConsole.MarkupLine(Red($"An operating system must be specified when using {LightYellow("--os")}"));
                AnsiConsole.Markup(Red($"Valid operating systems include : {LightYellow("windows/linux/macos")}"));
                throw new CommandException(".", 1, true);
            }
            if (!OperatingSystem.IsValidEnumValue(rawValue))
            {
                AnsiConsole.MarkupLine(Red($"{White(rawValue)} is not a valid operating system!"));
                AnsiConsole.Markup(Red($"Valid operating systems include : {LightYellow("windows/linux/macos")}"));
                throw new CommandException(".", 1, true);
            }
            return OperatingSystem.Parse(rawValue);
        }
    }

    public sealed class TransferSpeedUnitConverter : BindingConverter<TransferSpeedUnit>
    {
        public override TransferSpeedUnit Convert(string rawValue)
        {
            if (rawValue == null)
            {
                AnsiConsole.MarkupLine(Red($"A transfer speed unit must be specified when using {LightYellow("--unit")}"));
                AnsiConsole.Markup(Red($"Valid units include : {LightYellow("bits/bytes")}"));
                throw new CommandException(".", 1, true);
            }
            if (!TransferSpeedUnit.IsValidEnumValue(rawValue))
            {
                AnsiConsole.MarkupLine(Red($"{White(rawValue)} is not a valid transfer speed unit!"));
                AnsiConsole.Markup(Red($"Valid units include : {LightYellow("bits/bytes")}"));
                throw new CommandException(".", 1, true);
            }
            return TransferSpeedUnit.Parse(rawValue);
        }
    }
}