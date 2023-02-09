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

    /// <summary>
    /// Used to validate when an option flag has been specified, but no operating systems were specified.
    /// Ex. --os , should throw the validation error.
    /// </summary>
    public sealed class OperatingSystemValidator : BindingValidator<OperatingSystem[]>
    {
        public override BindingValidationError Validate(OperatingSystem[] value)
        {
            if (value.Length == 0)
            {
                AnsiConsole.MarkupLine(Red($"An operating system must be specified when using {LightYellow("--os")}"));
                AnsiConsole.Markup(Red($"Valid operating systems include : {LightYellow("windows/linux/macos")}"));
                throw new CommandException(".", 1, true);
            }
            return Ok();
        }
    }

    /// <summary>
    /// Used to validate that a value passed to an option flag is indeed a valid option.
    /// Ex. '--os android' will throw an exception since only windows/linux/macos are valid.
    /// </summary>
    public sealed class OperatingSystemConverter : BindingConverter<OperatingSystem>
    {
        public override OperatingSystem Convert(string rawValue)
        {
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
            // This will throw an error if a user specifies '--unit' but does not provide a value.  Does not work with List<T>
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


    public sealed class ConcurrencyValidator : BindingValidator<uint>
    {
        public override BindingValidationError Validate(uint value)
        {
            if (value == 0)
            {
                AnsiConsole.MarkupLine(Red($"{White(0)} is not a valid value for {LightYellow("--concurrency")}"));
                AnsiConsole.Markup(Red($"Please select a value between {LightYellow("1-100")}"));
                throw new CommandException(".", 1, true);
            }
            return Ok();
        }
    }
}