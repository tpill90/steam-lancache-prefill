namespace SteamPrefill.CliCommands
{
    /// <summary>
    /// Used to validate when an option flag has been specified, but no operating systems were specified.
    /// Ex. --os , should throw the validation error.
    /// </summary>
    public sealed class OperatingSystemValidator : BindingValidator<OperatingSystem[]>
    {
        public override BindingValidationError Validate(OperatingSystem[] value)
        {
            if (value.Length != 0)
            {
                return Ok();
            }

            AnsiConsole.MarkupLine(Red($"An operating system must be specified when using {LightYellow("--os")}"));
            AnsiConsole.Markup(Red($"Valid operating systems include : {LightYellow("windows/linux/macos")}"));
            throw new CommandException(".", 1, true);
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
            if (OperatingSystem.TryFromValue(rawValue, out _))
            {
                return OperatingSystem.FromValue(rawValue);
            }

            AnsiConsole.MarkupLine(Red($"{White(rawValue)} is not a valid operating system!"));
            AnsiConsole.Markup(Red($"Valid operating systems include : {LightYellow("windows/linux/macos")}"));
            throw new CommandException(".", 1, true);
        }
    }

    public sealed class ConcurrencyValidator : BindingValidator<uint>
    {
        public override BindingValidationError Validate(uint value)
        {
            if (value != 0)
            {
                return Ok();
            }

            AnsiConsole.MarkupLine(Red($"{White(0)} is not a valid value for {LightYellow("--concurrency")}"));
            AnsiConsole.Markup(Red($"Please select a value between {LightYellow("1-100")}"));
            throw new CommandException(".", 1, true);
        }
    }

    //TODO this pattern of these two is burdensome and difficult to understand.  Can this be abstracted at all?
    public sealed class PresetWorkloadValidator : BindingValidator<PresetWorkload[]>
    {
        public override BindingValidationError Validate(PresetWorkload[] value)
        {
            if (value.Length != 0)
            {
                return Ok();
            }

            AnsiConsole.MarkupLine(Red($"A preset must be specified when using {LightYellow("--preset")}"));
            AnsiConsole.Markup(Red($"Valid presets include : {LightYellow("Destiny2/Dota2")}"));
            throw new CommandException(".", 1, true);
        }
    }
    
    public sealed class PresetWorkloadConverter : BindingConverter<PresetWorkload>
    {
        public override PresetWorkload Convert(string rawValue)
        {
            // Making the first character always be uppercase, so that the parameter is case insensitive
            var upperCase = string.Concat(rawValue[0].ToString().ToUpper(), rawValue.AsSpan(1));
            if (PresetWorkload.TryFromName(upperCase, out _))
            {
                return PresetWorkload.FromName(upperCase);
            }

            AnsiConsole.MarkupLine(Red($"{White(rawValue)} is not a valid preset"));
            AnsiConsole.Markup(Red($"Valid presets include : {LightYellow("Destiny2/Dota2")}"));
            throw new CommandException(".", 1, true);
        }
    }
}