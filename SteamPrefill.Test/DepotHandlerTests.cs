using Moq;
using Spectre.Console.Testing;
using SteamKit2;
using SteamPrefill.Handlers;
using SteamPrefill.Handlers.Steam;
using SteamPrefill.Models;
using SteamPrefill.Models.Enums;
using Xunit;
using OperatingSystem = SteamPrefill.Models.Enums.OperatingSystem;

namespace SteamPrefill.Test
{
    public sealed class DepotHandlerTests
    {
        private readonly DepotHandler _depotHandler;

        public DepotHandlerTests()
        {
            Steam3Session steam3 = new Steam3Session(null);
            // User will always have access to every depot
            steam3.LicenseManager._userLicenses.OwnedDepotIds.Add(123);
            // User will always have access to this app
            steam3.LicenseManager._userLicenses.OwnedAppIds.Add(222);

            // Setting up a "valid" app info object
            var appKeyValues = new KeyValue
            {
                Children =
                {
                    new KeyValue("common")
                    {
                        Children = { new KeyValue("type", "game") }
                    }
                }
            };
            var appInfoHandlerMock = new Mock<AppInfoHandler>(null, null, null);
            appInfoHandlerMock.Setup(e => e.GetAppInfoAsync(It.IsAny<uint>()))
                          .Returns(Task.FromResult(new AppInfo(steam3, 222, appKeyValues)));

            _depotHandler = new DepotHandler(new TestConsole(), steam3, appInfoHandlerMock.Object, null, new DownloadArguments());
        }

        [Fact]
        public async Task UserDoesNotHaveDepotAccess_DepotIsFiltered()
        {
            var depotList = new List<DepotInfo>
            {
                new DepotInfo(new KeyValue("0"), 222) { DepotId = 777, ManifestId = 55 }
            };

            var filteredDepots = await _depotHandler.FilterDepotsToDownloadAsync(new DownloadArguments(), depotList);
            // Since the user has no access to any depots, we should expect this result to be empty
            Assert.Empty(filteredDepots);
        }

        [Fact]
        public async Task DepotHasNoMetadata_DepotIsIncluded()
        {
            var depotList = new List<DepotInfo>
            {
                // Depot is being setup without metadata
                new DepotInfo(new KeyValue("0"), 222) { DepotId = 123, ManifestId = 5555 }
            };

            var filteredDepots = await _depotHandler.FilterDepotsToDownloadAsync(new DownloadArguments(), depotList);
            // Since the depot has no metadata, it should always be included
            Assert.Single(filteredDepots);
        }

        [Fact]
        public async Task OperatingSystemDoesntMatch_DepotIsNotIncluded()
        {
            // Depot is for macos only
            var depotList = new List<DepotInfo>
            {
                new DepotInfo(new KeyValue("0"), 222)
                {
                    DepotId = 123,
                    ManifestId = 5555,
                    SupportedOperatingSystems = new List<OperatingSystem> { OperatingSystem.MacOS }
                }
            };

            var downloadArguments = new DownloadArguments { OperatingSystems = new List<OperatingSystem> { OperatingSystem.Windows } };
            var filteredDepots = await _depotHandler.FilterDepotsToDownloadAsync(downloadArguments, depotList);

            // We are only interested in windows depots, so we should expect the depot to be filtered
            Assert.Empty(filteredDepots);
        }

        [Theory]
        [InlineData("windows", "windows")]
        [InlineData("windows linux", "linux")]
        [InlineData("linux", "windows linux")]
        public async Task OperatingSystemMatches_DepotIsIncluded(string supportedOS, string downloadOS)
        {
            var depotList = new List<DepotInfo>
            {
                new DepotInfo(new KeyValue("0"), 222)
                {
                    DepotId = 123,
                    ManifestId = 5555,
                    SupportedOperatingSystems = supportedOS.Split(" ").Select(e => OperatingSystem.Parse(e)).ToList()
                }
            };

            var downloadArguments = new DownloadArguments
            {
                OperatingSystems = downloadOS.Split(" ").Select(e => OperatingSystem.Parse(e)).ToList()
            };
            var filteredDepots = await _depotHandler.FilterDepotsToDownloadAsync(downloadArguments, depotList);
            Assert.Single(filteredDepots);
        }


        [Fact]
        public async Task ArchitectureDoesntMatch_DepotIsNotIncluded()
        {
            // Depot is for 64 bit only
            var depotList = new List<DepotInfo>
            {
                new DepotInfo(new KeyValue("0"), 222) { DepotId = 123, ManifestId = 5555, Architecture = Architecture.x64 }
            };

            var filteredDepots = await _depotHandler.FilterDepotsToDownloadAsync(new DownloadArguments { Architecture = Architecture.x86 }, depotList);
            // We are only interested in 32 bit depots, so we should expect the depot to be filtered
            Assert.Empty(filteredDepots);
        }

        [Fact]
        public async Task ArchitectureMatches_DepotIsIncluded()
        {
            // Depot is for 64 bit only
            var depotList = new List<DepotInfo>
            {
                new DepotInfo(new KeyValue("0"), 222) { DepotId = 123, ManifestId = 5555, Architecture = Architecture.x64 }
            };

            var filteredDepots = await _depotHandler.FilterDepotsToDownloadAsync(new DownloadArguments { Architecture = Architecture.x64 }, depotList);
            // Since we want 64 bit depots, then we should expect the depot to be included
            Assert.Single(filteredDepots);
        }

        [Fact]
        public async Task LanguageDoesntMatch_DepotIsNotIncluded()
        {
            // Depot is for spanish
            var depotList = new List<DepotInfo>
            {
                new DepotInfo(new KeyValue("0"), 222) { DepotId = 123, ManifestId = 5555, Languages = new List<Language> { Language.Spanish } }
            };

            var filteredDepots = await _depotHandler.FilterDepotsToDownloadAsync(new DownloadArguments { Language = Language.English }, depotList);
            // We are only interested in english depots, so we should expect the depot to be filtered
            Assert.Empty(filteredDepots);
        }

        [Fact]
        public async Task LanguageMatches_DepotIsIncluded()
        {
            // Depot is for english
            var depotList = new List<DepotInfo>
            {
                new DepotInfo(new KeyValue("0"), 222) { DepotId = 123, ManifestId = 5555, Languages = new List<Language> { Language.English } }
            };

            var filteredDepots = await _depotHandler.FilterDepotsToDownloadAsync(new DownloadArguments { Language = Language.English }, depotList);
            // Since we want english depots, then we should expect the depot to be included
            Assert.Single(filteredDepots);
        }

        [Fact]
        public async Task LowViolenceDepots_AreFiltered()
        {
            var depotList = new List<DepotInfo>
            {
                new DepotInfo(new KeyValue("0"), 222) { DepotId = 123, ManifestId = 5555, LowViolence = true }
            };

            var filteredDepots = await _depotHandler.FilterDepotsToDownloadAsync(new DownloadArguments(), depotList);
            // Low violence depots should be expected to be filtered.
            Assert.Empty(filteredDepots);
        }
    }
}
