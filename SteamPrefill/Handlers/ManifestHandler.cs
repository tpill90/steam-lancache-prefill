namespace SteamPrefill.Handlers
{
    public sealed class ManifestHandler
    {
        private readonly IAnsiConsole _ansiConsole;
        private readonly CdnPool _cdnPool;
        private readonly Steam3Session _steam3Session;

        public ManifestHandler(IAnsiConsole ansiConsole, CdnPool cdnPool, Steam3Session steam3Session)
        {
            _ansiConsole = ansiConsole;
            _cdnPool = cdnPool;
            _steam3Session = steam3Session;
        }

        public async Task<Manifest> GetSingleManifestAsync(uint depotId, uint appId, ulong manifestId)
        {
            if (ManifestIsCached(depotId, appId, manifestId))
            {
                return Manifest.LoadFromFile(GetManifestFileName(depotId, appId, manifestId));
            }

            _ansiConsole.LogMarkupVerbose($"Downloading manifest {LightYellow(manifestId)} for depot {Cyan(depotId)}");

            ManifestRequestCode manifestRequestCode = await GetManifestRequestCodeAsync(depotId, appId, manifestId);

            Server server = _cdnPool.TakeConnection();
            DepotManifest manifest = await _steam3Session.CdnClient.DownloadManifestAsync(depotId, manifestId, manifestRequestCode.Code, server);
            if (manifest == null)
            {
                throw new ManifestException($"Unable to download manifest for depot   Manifest request received no response.");
            }
            _cdnPool.ReturnConnection(server);

            var protoManifest = new Manifest(manifest, depotId, manifestId);
            if (AppConfig.NoLocalCache)
            {
                return protoManifest;
            }

            protoManifest.SaveToFile(GetManifestFileName(depotId, appId, manifestId));
            return protoManifest;
        }

        private async Task<ManifestRequestCode> GetManifestRequestCodeAsync(uint depotId, uint appId, ulong manifestId)
        {
            ulong manifestRequestCode = await _steam3Session.SteamContent.GetManifestRequestCode(depotId, appId, manifestId, "public")
                                                                         // Adding an additional timeout to this SteamKit method.  I have a feeling that this is not properly timing out
                                                                         // for some users.
                                                                         .WaitAsync(TimeSpan.FromSeconds(90));

            // If we could not get the manifest code, this is a fatal error, as it we can't download the manifest without it.
            if (manifestRequestCode == 0)
            {
                throw new ManifestException($"No manifest request code was returned for {depotId} {manifestId}");
            }

            return new ManifestRequestCode
            {
                Code = manifestRequestCode,
                RetrievedAt = DateTime.Now
            };
        }

        private bool ManifestIsCached(uint depotId, uint appId, ulong manifestId)
        {
            return !AppConfig.NoLocalCache && File.Exists(GetManifestFileName(depotId, appId, manifestId));
        }

        private string GetManifestFileName(uint depotId, uint appId, ulong manifestId)
        {
            return $"{AppConfig.TempDir}/{appId}_{depotId}_{manifestId}.bin";
        }
    }
}