namespace SteamPrefill.CliCommands
{
    #region Operating system

    /// <summary>
    /// Checks to make sure that at least one value has been passed to the --os option.
    /// Ex. --os , should throw the validation error.
    /// </summary>
    public sealed class OperatingSystemValidator : BindingValidator<OperatingSystem[]>
    {
        public override BindingValidationError Validate(OperatingSystem[] value)
        {
            if (value.Any())
            {
                return Ok();
            }

            AnsiConsole.MarkupLine(Red($"An operating system must be specified when using {LightYellow("--os")}"));
            AnsiConsole.Markup(Red($"Valid operating systems include : {LightYellow("windows/linux/macos")}"));
            throw new CommandException(".", 1, true);
        }
    }

    /// <summary>
    /// Used to convert a value passed to an option flag into an OperatingSystem type.
    /// Will additionally validate the value is valid, and show an error if it isn't.
    /// Ex. '--os freebsd' will throw an exception since only windows/linux/macos are valid.
    /// </summary>
    public sealed class OperatingSystemConverter : BindingConverter<OperatingSystem>
    {
        public override OperatingSystem Convert(string rawValue)
        {
            // Successful conversion
            if (OperatingSystem.TryFromValue(rawValue, out _))
            {
                return OperatingSystem.FromValue(rawValue);
            }

            AnsiConsole.MarkupLine(Red($"{White(rawValue)} is not a valid operating system!"));
            AnsiConsole.Markup(Red($"Valid operating systems include : {LightYellow("windows/linux/macos")}"));
            throw new CommandException(".", 1, true);
        }
    }

    #endregion

    #region Benchmark

    /// <summary>
    /// Validates that concurrency can't be set to 0.
    /// </summary>
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

    /// <summary>
    /// Checks to make sure that at least one value has been passed to the --preset option.
    /// </summary>
    public sealed class PresetWorkloadValidator : BindingValidator<PresetWorkload[]>
    {
        public override BindingValidationError Validate(PresetWorkload[] value)
        {
            if (value.Any())
            {
                return Ok();
            }

            AnsiConsole.MarkupLine(Red($"A preset must be specified when using {LightYellow("--preset")}"));
            AnsiConsole.Markup(Red($"Valid presets include : {LightYellow("SmallChunks/LargeChunks")}"));
            throw new CommandException(".", 1, true);
        }
    }

    /// <summary>
    /// Used to convert a value passed to an option flag into an PresetWorkload type.
    /// Will additionally validate the value is valid, and show an error if it isn't.
    /// Ex. '--preset medium' will throw an exception since only SmallChunks/LargeChunks are valid.
    /// </summary>
    public sealed class PresetWorkloadConverter : BindingConverter<PresetWorkload>
    {
        public override PresetWorkload Convert(string rawValue)
        {
            if (PresetWorkload.TryFromName(rawValue, StringComparison.InvariantCultureIgnoreCase, out _))
            {
                return PresetWorkload.FromName(rawValue, StringComparison.InvariantCultureIgnoreCase);
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

            // Checking to make sure that the value provided is one of the enum's values
            rawValue = rawValue.ToLower();
            if (SortOrder.TryFromValue(rawValue, out var _))
            {
                return SortOrder.FromValue(rawValue);
            }

            AnsiConsole.MarkupLine(Red($"{White(rawValue)} is not a valid sort order!"));
            AnsiConsole.Markup(Red($"Valid sort orders include : {LightYellow("ascending/descending")}"));
            throw new CommandException(".", 1, true);
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
            if (SortColumn.TryFromValue(rawValue, out var _))
            {
                return SortColumn.FromValue(rawValue);
            }

            AnsiConsole.MarkupLine(Red($"{White(rawValue)} is not a valid value for {LightYellow("--sort-by")}"));
            AnsiConsole.Markup(Red($"Valid options include : {LightYellow("app/size")}"));
            throw new CommandException(".", 1, true);
        }
    }

    #endregion
}