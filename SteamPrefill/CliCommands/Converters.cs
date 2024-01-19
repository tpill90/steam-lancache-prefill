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
            if (!OperatingSystem.TryFromValue(rawValue, out var _))
            {
                AnsiConsole.MarkupLine(Red($"{White(rawValue)} is not a valid operating system!"));
                AnsiConsole.Markup(Red($"Valid operating systems include : {LightYellow("windows/linux/macos")}"));
                throw new CommandException(".", 1, true);
            }
            return OperatingSystem.FromValue(rawValue);
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

    //TODO this pattern of these two is burdensome and difficult to understand.  Can this be abstracted at all?
    //TODO comment
    public sealed class PresetWorkloadValidator : BindingValidator<PresetWorkload[]>
    {
        public override BindingValidationError Validate(PresetWorkload[] value)
        {
            if (value.Length == 0)
            {
                AnsiConsole.MarkupLine(Red($"A preset must be specified when using {LightYellow("--preset")}"));
                AnsiConsole.Markup(Red($"Valid presets include : {LightYellow("Destiny2/Dota2")}"));
                throw new CommandException(".", 1, true);
            }
            return Ok();
        }
    }

    //TODO document
    public sealed class PresetWorkloadConverter : BindingConverter<PresetWorkload>
    {
        public override PresetWorkload Convert(string rawValue)
        {
            if (!PresetWorkload.TryFromName(rawValue, out var _))
            {
                AnsiConsole.MarkupLine(Red($"{White(rawValue)} is not a valid preset"));
                AnsiConsole.Markup(Red($"Valid presets include : {LightYellow("Destiny2/Dota2")}"));
                throw new CommandException(".", 1, true);
            }
            return PresetWorkload.FromName(rawValue);
        }
    }
}