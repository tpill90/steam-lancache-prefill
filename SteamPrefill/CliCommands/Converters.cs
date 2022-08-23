namespace SteamPrefill.CliCommands
{
    public class IpAddressConverter : BindingConverter<IPAddress>
    {
        public override IPAddress Convert(string rawValue)
        {
            if (rawValue == null)
            {
                AnsiConsole.Markup(Red($"An IP address must be specified when using {LightYellow("--dns-override")}"));
                throw new CommandException(".", 1, true);
            }
            if (!IPAddress.TryParse(rawValue, out var ipAddress))
            {
                AnsiConsole.MarkupLine(Red($"{White(rawValue)} is not a valid IP address!"));
                AnsiConsole.Markup(Red($"The specified address must be in the format {LightYellow("XXX.XXX.XXX.XXX")}, e.g. {LightGreen("192.168.1.22")}"));
                throw new CommandException(".", 1, true);
            }
            return ipAddress;
        }
    }

    //TODO possibly consider implementing something similar in CliFx, so that boolean flags don't show 'Default: "False"'
    public class NullableBoolConverter : BindingConverter<bool?>
    {
        // Required in order to prevent CliFx from showing the unnecessary 'Default: "False"' text for boolean flags
        public override bool? Convert(string rawValue)
        {
            return true;
        }
    }

    public class TransferSpeedUnitConverter : BindingConverter<TransferSpeedUnit>
    {
        public override TransferSpeedUnit Convert(string rawValue)
        {
            if (rawValue == null)
            {
                AnsiConsole.MarkupLine(Red($"A transfer speed unit must be specified when using {LightYellow("--unit")}"));
                //TODO this will easily get out of date.  Should EnumBase have a way of generating this?
                AnsiConsole.Markup(Red($"Valid units include : {LightYellow("bits/bytes")}"));
                throw new CommandException(".", 1, true);
            }
            if (!TransferSpeedUnit.IsValidEnumValue(rawValue))
            {
                AnsiConsole.MarkupLine(Red($"{White(rawValue)} is not a valid transfer speed unit!"));
                //TODO this will easily get out of date.  Should EnumBase have a way of generating this?
                AnsiConsole.Markup(Red($"Valid units include : {LightYellow("bits/bytes")}"));
                throw new CommandException(".", 1, true);
            }
            return TransferSpeedUnit.Parse(rawValue);
        }
    }
}
