// ReSharper disable MemberCanBePrivate.Global - Properties used as parameters can't be private with CliFx, otherwise they won't work.
namespace SteamPrefill.CliCommands
{
    [UsedImplicitly]
    [Command("prefill", Description = "Downloads the latest version of one or more specified app(s)." +
                                           "  Automatically includes apps selected using the 'select-apps' command")]
    public class PrefillCommand : ICommand
    {

#if DEBUG // Experimental, debugging only
        [CommandOption("app")]
        public IReadOnlyList<uint> AppIds { get; init; }
#endif

        [CommandOption("all", Description = "Prefills all currently owned apps", Converter = typeof(NullableBoolConverter))]
        public bool? DownloadAllOwnedGames { get; init; }

        //TODO remove in a future version
        [CommandOption("dns-override", 'd',
            Description = "Deprecated, will be removed in a future version.  Manually specifies the Lancache IP, used to prefill on the Lancache server.  Ex, '192.168.1.111'",
            Converter = typeof(IpAddressConverter))]
        public IPAddress OverrideLancacheIp { get; init; }

        [CommandOption("force", 'f', 
            Description = "Forces the prefill to always run, overrides the default behavior of only prefilling if a newer version is available.", 
            Converter = typeof(NullableBoolConverter))]
        public bool? Force { get; init; }

        [CommandOption("nocache",
            Description = "Skips using locally cached files.  Saves disk space, at the expense of slower subsequent runs.",
            Converter = typeof(NullableBoolConverter))]
        public bool? NoLocalCache { get; init; }

        [CommandOption("unit", 
            Description = "Specifies which unit to use to display download speed.  Can be either bits/bytes.  Default: bits",
            Converter = typeof(TransferSpeedUnitConverter))]
        public TransferSpeedUnit TransferSpeedUnit { get; init; }

        private IAnsiConsole _ansiConsole;
        public async ValueTask ExecuteAsync(IConsole console)
        {
            _ansiConsole = console.CreateAnsiConsole();

            await UpdateChecker.CheckForUpdatesAsync(typeof(Program), "tpill90/steam-lancache-prefill", AppConfig.CacheDir);

            var downloadArgs = new DownloadArguments
            {
                Force = Force ?? default(bool),
                NoCache = NoLocalCache ?? default(bool),
                TransferSpeedUnit = TransferSpeedUnit ?? TransferSpeedUnit.Bits
            };

            if (OverrideLancacheIp != null)
            {
                _ansiConsole.MarkupLine(LightYellow($" Warning!  {White("--dns-override")} is no longer required, and will be removed in a future version!\n" +
                                                    " SteamPrefill will automatically detect the Lancache server IP if running on the same machine.\n"));
            }

            using var steamManager = new SteamManager(_ansiConsole, downloadArgs);
            ValidateSelectedAppIds(steamManager);

            try
            {
                steamManager.Initialize();

                var manualIds = new List<uint>();
                #if DEBUG // Experimental, debugging only
                if (AppIds != null)
                {
                    manualIds.AddRange(AppIds);
                }
                #endif

                await steamManager.DownloadMultipleAppsAsync(DownloadAllOwnedGames ?? default(bool), manualIds);
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

        private void ValidateSelectedAppIds(SteamManager steamManager)
        {
            var userSelectedApps = steamManager.LoadPreviouslySelectedApps();

#if DEBUG
            if (AppIds != null && AppIds.Any())
            {
                return;
            }
#endif

            if ((DownloadAllOwnedGames ?? default(bool)) || userSelectedApps.Any())
            {
                return;
            }
            _ansiConsole.MarkupLine(Red("No apps have been selected for prefill! At least 1 app is required!"));
            _ansiConsole.MarkupLine(Red($"Use the {Cyan("select-apps")} command to interactively choose which apps to prefill. "));
            _ansiConsole.MarkupLine("");
            _ansiConsole.Markup(Red($"Alternatively, the flag {LightYellow("--all")} can be specified to prefill all owned apps"));
            throw new CommandException(".", 1, true);
        }

    }
}
