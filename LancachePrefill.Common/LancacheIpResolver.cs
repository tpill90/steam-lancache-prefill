namespace LancachePrefill.Common
{
    /// <summary>
    /// Attempts to automatically resolve the Lancache's IP address,
    /// allowing users to be able to run the prefill on the same machine as the Lancache.
    ///
    /// Will automatically try to detect the Lancache through the poisoned DNS entries, however if that is not possible it will then check
    /// 'localhost' to see if the Lancache is available locally.  If the server is not available on 'localhost', then 172.17.0.1 will be checked to see if
    /// the prefill is running from a docker container
    /// </summary>
    public static class LancacheIpResolver
    {
        private static IAnsiConsole _ansiConsole;

        public static async Task<string> ResolveLancacheIpAsync(IAnsiConsole ansiConsole, string cdnUrl)
        {
            if (_ansiConsole == null)
            {
                _ansiConsole = ansiConsole;
            }
            string detectedServer = await _ansiConsole.StatusSpinner().StartAsync("Detecting Lancache server...", async context =>
            {
                return await DetectLancacheServerAsync(cdnUrl);
            });

            if (detectedServer != null)
            {
                _ansiConsole.LogMarkupLine($"Detected Lancache server at {Cyan(detectedServer)}!");
                return detectedServer;
            }

            await DetectPublicIpAsync(cdnUrl);
            return cdnUrl;
        }

        private static async Task<string> DetectLancacheServerAsync(string cdnUrl)
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            // Tries to resolve poisoned DNS record, then localhost, then Docker's host
            var possibleLancacheUrls = new List<string> { cdnUrl, "127.0.0.1", "172.17.0.1" };

            foreach (var url in possibleLancacheUrls)
            {
                var ipAddresses = await Dns.GetHostAddressesAsync(url);
                if (!ipAddresses.Any(e => e.IsInternal()))
                {
                    continue;
                }

                try
                {
                    // If the IP resolves to a private subnet, then we want to query the Lancache server to see if it is actually there.
                    var response = await httpClient.GetAsync(new Uri($"http://{url}/lancache-heartbeat"));
                    if (response.Headers.Contains("X-LanCache-Processed-By"))
                    {
                        return url;
                    }
                    else if (!response.Headers.Contains("X-LanCache-Processed-By") && url == cdnUrl)
                    {
                        _ansiConsole.MarkupLine(Red($" Error!  {White(cdnUrl)} is resolving to a private IP address {Cyan($"({ipAddresses.First()})")},\n" +
                                                    " however no Lancache can be found at that address.\n" +
                                                    " Please check your configuration, and try again.\n"));
                        throw new LancacheNotFoundException($"No Lancache server detected at {ipAddresses.First()}");
                    }
                }
                catch (Exception e) when (e is HttpRequestException | e is TaskCanceledException)
                {
                    // Catching target machine refused connection + timeout exceptions, so we can try the next address
                }
            }
            return null;
        }

        private static async Task DetectPublicIpAsync(string cdnUrl)
        {
            var ipAddresses = await Dns.GetHostAddressesAsync(cdnUrl);
            if (ipAddresses.Any(e => e.IsInternal()))
            {
                return;
            }

            // If a public IP address is resolved, then it means that the Lancache is not configured properly, and we would end up downloading from the internet.
            // This will prompt a user to see if they still want to continue, as downloading from the internet could still be a good download speed test.
            _ansiConsole.MarkupLine(LightYellow($" Warning!  {White(cdnUrl)} is resolving to a public IP address {Cyan($"({ipAddresses.First()})")}.\n" +
                                                " Prefill will download directly from the internet, and will not be cached by Lancache.\n"));

            var publicDownloadOverride = _ansiConsole.Prompt(new SelectionPrompt<bool>()
                                                             .Title("Continue anyway?")
                                                             .AddChoices(true, false)
                                                             .UseConverter(e => e == false ? "No" : "Yes"));

            if (publicDownloadOverride == false)
            {
                throw new UserCancelledException("User cancelled download!");
            }
        }
    }
}