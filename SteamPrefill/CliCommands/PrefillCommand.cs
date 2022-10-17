// ReSharper disable MemberCanBePrivate.Global - Properties used as parameters can't be private with CliFx, otherwise they won't work.
namespace SteamPrefill.CliCommands
{
    //TODO need to rethink the verbiage between 'app' and 'game' on the user facing docs
    [UsedImplicitly]
    [Command("prefill", Description = "Downloads the latest version of one or more specified app(s)." +
                                           "  Automatically includes apps selected using the 'select-apps' command")]
    public class PrefillCommand : ICommand
    {

#if DEBUG // Experimental, debugging only
        [CommandOption("app", Description = "Debugging only.")]
        public IReadOnlyList<uint> AppIds { get; init; }

        [CommandOption("no-download", Description = "Debugging only.", Converter = typeof(NullableBoolConverter))]
        public bool? NoDownload
        {
            get => AppConfig.SkipDownloads;
            init => AppConfig.SkipDownloads = value ?? default(bool);
        }
#endif

        [CommandOption("all", Description = "Prefills all currently owned games", Converter = typeof(NullableBoolConverter))]
        public bool? DownloadAllOwnedGames { get; init; }

        [CommandOption("recent", Description = "Prefill will include all games played in the last 2 weeks.", Converter = typeof(NullableBoolConverter))]
        public bool? PrefillRecentGames { get; init; }

        [CommandOption("top", Description = "Prefills the most popular games by player count, over the last 2 weeks.  Default: 50")]
        public int? PrefillPopularGames
        {
            get => _prefillPopularGames;
            // Need to use a setter in order to set a default value, so that the default will only be used when the option flag is specified
            set => _prefillPopularGames = value ?? 50;
        }

        [CommandOption("force", 'f',
            Description = "Forces the prefill to always run, overrides the default behavior of only prefilling if a newer version is available.",
            Converter = typeof(NullableBoolConverter))]
        public bool? Force { get; init; }

        [CommandOption("nocache",
            Description = "Skips using locally cached files. Saves disk space, at the expense of slower subsequent runs.",
            Converter = typeof(NullableBoolConverter))]
        public bool? NoLocalCache { get; init; }

        [CommandOption("verbose", Description = "Produces more detailed log output. Will output logs for games are already up to date.", Converter = typeof(NullableBoolConverter))]
        public bool? Verbose
        {
            get => AppConfig.VerboseLogs;
            init => AppConfig.VerboseLogs = value ?? default(bool);
        }

        [CommandOption("unit",
            Description = "Specifies which unit to use to display download speed.  Can be either bits/bytes.",
            Converter = typeof(TransferSpeedUnitConverter))]
        public TransferSpeedUnit TransferSpeedUnit { get; init; } = TransferSpeedUnit.Bits;

        [CommandOption("no-ansi",
            Description = "Application output will be in plain text.  " +
                          "Should only be used if terminal does not support Ansi Escape sequences, or when redirecting output to a file.",
            Converter = typeof(NullableBoolConverter))]
        public bool? NoAnsiEscapeSequences { get; init; }

        private IAnsiConsole _ansiConsole;
        private int? _prefillPopularGames;

        public async ValueTask ExecuteAsync(IConsole console)
        {
            _ansiConsole = console.CreateAnsiConsole();
            // Property must be set to false in order to disable ansi escape sequences
            _ansiConsole.Profile.Capabilities.Ansi = !NoAnsiEscapeSequences ?? true;

            await UpdateChecker.CheckForUpdatesAsync(typeof(Program), "tpill90/steam-lancache-prefill", AppConfig.CacheDir);

            var downloadArgs = new DownloadArguments
            {
                Force = Force ?? default(bool),
                NoCache = NoLocalCache ?? default(bool),
                TransferSpeedUnit = TransferSpeedUnit
            };

            using var steamManager = new SteamManager(_ansiConsole, downloadArgs);
            ValidateUserHasSelectedApps(steamManager);
            ValidatePopularGameCount();

            try
            {
                await steamManager.InitializeAsync();

                var manualIds = new List<uint>();
#if DEBUG 
                // Experimental, debugging only
                if (AppIds != null)
                {
                    manualIds.AddRange(AppIds);
                }
#endif
                await steamManager.DownloadMultipleAppsAsync(DownloadAllOwnedGames ?? default(bool),
                                                             PrefillRecentGames ?? default(bool),
                                                             PrefillPopularGames,
                                                             manualIds);
            }
            catch (TimeoutException e)
            {
                _ansiConsole.MarkupLine("\n");
                if (e.StackTrace.Contains(nameof(UserAccountStore.GetUsernameAsync)))
                {
                    _ansiConsole.MarkupLine(Red("Timed out while waiting for username entry"));
                }
                if (e.StackTrace.Contains(nameof(AnsiConsoleExtensions.ReadPassword)))
                {
                    _ansiConsole.MarkupLine(Red("Timed out while waiting for password entry"));
                }
                _ansiConsole.WriteException(e, ExceptionFormats.ShortenPaths);
            }
            catch (Exception e)
            {
                _ansiConsole.WriteException(e, ExceptionFormats.ShortenPaths);
            }
            finally
            {
                steamManager.Shutdown();
            }
        }

        // Validates that the user has selected at least 1 app
        private void ValidateUserHasSelectedApps(SteamManager steamManager)
        {
            var userSelectedApps = steamManager.LoadPreviouslySelectedApps();

#if DEBUG
            if (AppIds != null && AppIds.Any())
            {
                return;
            }
#endif

            if ((DownloadAllOwnedGames ?? default(bool)) || (PrefillRecentGames ?? default(bool)) || PrefillPopularGames != null || userSelectedApps.Any())
            {
                return;
            }

            _ansiConsole.MarkupLine(Red("No apps have been selected for prefill! At least 1 app is required!"));
            _ansiConsole.MarkupLine(Red($"Use the {Cyan("select-apps")} command to interactively choose which apps to prefill. "));
            _ansiConsole.MarkupLine("");
            _ansiConsole.Markup(Red($"Alternatively, the flag {LightYellow("--all")} can be specified to prefill all owned apps"));
            throw new CommandException(".", 1, true);
        }

        private void ValidatePopularGameCount()
        {
            if (PrefillPopularGames != null && PrefillPopularGames < 1 || PrefillPopularGames > 100)
            {
                _ansiConsole.Markup(Red($"Value for {LightYellow("--top")} must be in the range 1-100"));
                throw new CommandException(".", 1, true);
            }
        }
    }
}
