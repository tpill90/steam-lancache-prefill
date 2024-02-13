namespace SteamPrefill.CliCommands
{
    //TODO these need to have better documentation overall.  There are multiple "types" of these converters and validators, and they all cover different scenarios
    //TODO go through all of these converters/validators and make sure their output messages are consistent between them all
    #region Operating system

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
            //TODO case insensitive
            if (!OperatingSystem.TryFromValue(rawValue, out var _))
            {
                AnsiConsole.MarkupLine(Red($"{White(rawValue)} is not a valid operating system!"));
                AnsiConsole.Markup(Red($"Valid operating systems include : {LightYellow("windows/linux/macos")}"));
                throw new CommandException(".", 1, true);
            }
            return OperatingSystem.FromValue(rawValue);
        }
    }

    #endregion

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
                AnsiConsole.Markup(Red($"Valid presets include : {LightYellow("SmallChunks/BigChunks")}"));
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
            //TODO case insensitive
            if (!PresetWorkload.TryFromName(rawValue, out var _))
            {
                AnsiConsole.MarkupLine(Red($"{White(rawValue)} is not a valid preset"));
                AnsiConsole.Markup(Red($"Valid presets include : {LightYellow("SmallChunks/BigChunks")}"));
                throw new CommandException(".", 1, true);
            }
            return PresetWorkload.FromName(rawValue);
        }
    }

    #region Status command validators

    public sealed class SortOrderConverter : BindingConverter<SortOrder>
    {
        public override SortOrder Convert(string rawValue)
        {
            // This will throw an error if a user specifies '--sort-order' but does not provide a value.
            if (rawValue == null)
            {
                AnsiConsole.MarkupLine(Red($"A sort order must be provided when using {LightYellow("--sort-order")}"));
                AnsiConsole.Markup(Red($"Valid sort orders include : {LightYellow("ascending/descending")}"));
                throw new CommandException(".", 1, true);
            }

            rawValue = rawValue.ToLower();
            if (!SortOrder.TryFromValue(rawValue, out var _))
            {
                AnsiConsole.MarkupLine(Red($"{White(rawValue)} is not a valid sort order!"));
                AnsiConsole.Markup(Red($"Valid sort orders include : {LightYellow("ascending/descending")}"));
                throw new CommandException(".", 1, true);
            }
            return SortOrder.FromValue(rawValue);
        }
    }

    public sealed class SortColumnConverter : BindingConverter<SortColumn>
    {
        public override SortColumn Convert(string rawValue)
        {
            // This will throw an error if a user specifies '--sort-by' but does not provide a value.
            if (rawValue == null)
            {
                AnsiConsole.MarkupLine(Red($"A value must be provided when using {LightYellow("--sort-by")}"));
                AnsiConsole.Markup(Red($"Valid options include : {LightYellow("app/size")}"));
                throw new CommandException(".", 1, true);
            }

            rawValue = rawValue.ToLower();
            if (!SortColumn.TryFromValue(rawValue, out var _))
            {
                AnsiConsole.MarkupLine(Red($"{White(0)} is not a valid value for {LightYellow("--sort-by")}"));
                AnsiConsole.Markup(Red($"Valid options include : {LightYellow("app/size")}"));
                throw new CommandException(".", 1, true);
            }
            return SortColumn.FromValue(rawValue);
        }
    }

    #endregion
}