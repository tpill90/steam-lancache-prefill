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
            //TODO case insensitive
            rawValue = rawValue.ToLower();
            if (!OperatingSystem.TryFromValue(rawValue, out _))
            {
                AnsiConsole.MarkupLine(Red($"{White(rawValue)} is not a valid operating system!"));
                AnsiConsole.Markup(Red($"Valid operating systems include : {LightYellow("windows/linux/macos")}"));
                throw new CommandException(".", 1, true);
            }
            return OperatingSystem.FromValue(rawValue);
        }
    }

    #endregion

    #region Benchmark

    public sealed class ConcurrencyValidator : BindingValidator<uint>
    {
        public override BindingValidationError Validate(uint value)
        {
            if (value != 0)
            {
                return Ok();
            }

            AnsiConsole.MarkupLine(Red($"{White(0)} is not a valid value for {LightYellow("--concurrency")}"));
            AnsiConsole.Markup(Red($"Please enter a value between {LightYellow("1-100")}"));
            throw new CommandException(".", 1, true);
        }
    }

    //TODO this pattern of these two is burdensome and difficult to understand.  Can this be abstracted at all?
    /// <summary>
    /// Checks to make sure that at least one value has been passed to the --preset option.
    /// </summary>
    public sealed class PresetWorkloadValidator : BindingValidator<PresetWorkload[]>
    {
        public override BindingValidationError Validate(PresetWorkload[] value)
        {
            if (value.Length != 0)
            {
                return Ok();
            }

            AnsiConsole.MarkupLine(Red($"A preset must be specified when using {LightYellow("--preset")}"));
            AnsiConsole.Markup(Red($"Valid presets include : {LightYellow("SmallChunks/LargeChunks")}"));
            throw new CommandException(".", 1, true);
        }
    }

    public sealed class PresetWorkloadConverter : BindingConverter<PresetWorkload>
    {
        public override PresetWorkload Convert(string rawValue)
        {
            //TODO make completely case insensitive
            // Making the first character always be uppercase, so that the parameter is case insensitive
            var upperCase = string.Concat(rawValue[0].ToString().ToUpper(), rawValue.AsSpan(1));
            if (PresetWorkload.TryFromName(upperCase, out _))
            {
                return PresetWorkload.FromName(upperCase);
            }

            AnsiConsole.MarkupLine(Red($"{White(rawValue)} is not a valid preset"));
            AnsiConsole.Markup(Red($"Valid presets include : {LightYellow("SmallChunks/LargeChunks")}"));
            throw new CommandException(".", 1, true);
        }
    }

    #endregion

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

    /// <summary>
    /// Validates that the user has provided a valid value when using the '--sort-by' option.
    /// </summary>
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

            // Checking to make sure that the value provided is one of the enum's values
            rawValue = rawValue.ToLower();
            if (!SortColumn.TryFromValue(rawValue, out var _))
            {
                AnsiConsole.MarkupLine(Red($"{White(rawValue)} is not a valid value for {LightYellow("--sort-by")}"));
                AnsiConsole.Markup(Red($"Valid options include : {LightYellow("app/size")}"));
                throw new CommandException(".", 1, true);
            }
            return SortColumn.FromValue(rawValue);
        }
    }

    #endregion
}