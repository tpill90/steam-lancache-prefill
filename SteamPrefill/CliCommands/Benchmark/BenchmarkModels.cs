using Color = Spectre.Console.Color;

namespace SteamPrefill.CliCommands.Benchmark
{
    [ProtoContract(SkipConstructor = true)]
    public sealed class BenchmarkWorkload
    {
        [ProtoMember(1)]
        public ConcurrentBag<AppQueuedRequests> QueuedAppsList { get; init; }

        //TODO document why this is needed
        [ProtoMember(2)]
        public List<CdnServerShim> ServerShimList { get; init; }
        public ConcurrentStack<Server> CdnServerList => ServerShimList.Select(e => _mapper.Map<CdnServerShim, Server>(e)).ToConcurrentStack();

        public List<QueuedRequest> AllQueuedRequests => QueuedAppsList.SelectMany(e => e.QueuedRequests).ToList();

        private ByteSize? _totalDownloadSize;
        public ByteSize TotalDownloadSize
        {
            get
            {
                if (_totalDownloadSize == null)
                {
                    _totalDownloadSize = ByteSize.FromBytes(QueuedAppsList.Sum(e => e.TotalBytes));
                }
                return _totalDownloadSize.Value;
            }
        }

        public long TotalFiles => QueuedAppsList.Sum(e => e.FileCount);
        public string TotalFilesFormatted => TotalFiles.ToString("n0");

        public ByteSize AverageChunkSize => ByteSize.FromBytes((double)TotalDownloadSize.Bytes / (double)TotalFiles);
        public string AverageChunkSizeFormatted => AverageChunkSize.ToBinaryString();

        private static IMapper _mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Server, CdnServerShim>();
            cfg.CreateMap<CdnServerShim, Server>();
        }).CreateMapper();

        public BenchmarkWorkload(ConcurrentBag<AppQueuedRequests> queuedAppsList, ConcurrentStack<Server> cdnServers)
        {
            QueuedAppsList = queuedAppsList;
            _totalDownloadSize = ByteSize.FromBytes(QueuedAppsList.Sum(e => e.TotalBytes));
            ServerShimList = cdnServers.Select(e => _mapper.Map<Server, CdnServerShim>(e)).ToList();
        }

        #region Summary output

        public void PrintSummary(IAnsiConsole ansiConsole)
        {
            // Setting up final formatting, to make sure padding and alignment is correct
            var grid = new Grid()
                       .AddColumn(new GridColumn())
                       // Summary Table
                       .AddRow(White(Underline("Benchmark workload summary")))
                       .AddRow(BuildSummaryTable())
                       // Request distribution
                       .AddRow(White(Underline("Chunk size distribution")))
                       .AddEmptyRow()
                       .AddRow(BuildRequestSizeChart());

            ansiConsole.Write(new Padder(grid, new Padding(1, 0)));
            ansiConsole.WriteLine();
        }

        private Table BuildSummaryTable()
        {
            // Building out summary table
            var table = new Table { Border = TableBorder.MinimalHeavyHead };

            // Header
            table.AddColumn(new TableColumn(Cyan("App")));
            table.AddColumn(new TableColumn(White("Id")).RightAligned());
            table.AddColumn(new TableColumn(MediumPurple("Download Size")).RightAligned());
            table.AddColumn(new TableColumn(LightYellow("Total Chunks")).RightAligned());
            table.AddColumn(new TableColumn(LightGreen("Average Chunk Size")).RightAligned());

            // Add Rows, but only if the number of entries is manageable, otherwise it will be absurdly large and not readable on one screen
            if (QueuedAppsList.Count <= 20)
            {
                foreach (var fileList in QueuedAppsList)
                {
                    table.AddRow(fileList.AppName, fileList.AppId.ToString(), fileList.TotalBytesFormatted, fileList.FormattedFileCount, fileList.AverageChunkSizeFormatted);
                }
            }

            // Summary footer
            table.Columns[2].Footer = new Markup(Bold(White(TotalDownloadSize.ToBinaryString())));
            table.Columns[3].Footer = new Markup(Bold(White(TotalFilesFormatted)));
            table.Columns[4].Footer = new Markup(Bold(White(AverageChunkSizeFormatted)));
            return table;
        }

        /// <summary>
        /// Generates a bar chart of chunk size distribution
        /// </summary>
        private BarChart BuildRequestSizeChart()
        {
            var byteRange = ByteSize.FromKibiBytes(256).Bytes;
            var lookup = AllQueuedRequests.ToLookup(e => (int)(e.CompressedLength / byteRange));

            // Enumerable.Range() is similar to Powershell's 0..10 range syntax
            var bucketedRequests = Enumerable.Range(0, lookup.Max(x => x.Key) + 1)
                                             .Select(e => new
                                             {
                                                 Lower = ByteSize.FromBytes(e * byteRange),
                                                 Upper = ByteSize.FromBytes((e + 1) * byteRange),
                                                 Range = $"{ByteSize.FromBytes(e * byteRange).KibiBytes} - {ByteSize.FromBytes((e + 1) * byteRange).ToBinaryString()}",
                                                 Count = lookup[e].Count()
                                             })
                                             .Where(e => e.Count > 0)
                                             .ToList();

            var barChart = new BarChart().Width(120);
            foreach (var result in bucketedRequests)
            {
                Color color = Color.Default;
                if (result.Upper <= ByteSize.FromKibiBytes(256))
                {
                    color = SpectreColors.LightRed;
                }
                else if (result.Lower >= ByteSize.FromKibiBytes(896))
                {
                    color = SpectreColors.LightGreen;
                }
                barChart.AddItem(result.Range, result.Count, color);
            }

            return barChart;
        }

        #endregion

        #region Serialization

        public static BenchmarkWorkload LoadFromFile(string filename)
        {
            if (!File.Exists(filename))
            {
                return null;
            }
            using var fs = File.Open(filename, FileMode.Open);
            return Serializer.Deserialize<BenchmarkWorkload>(fs);
        }

        public void SaveToFile(string filename)
        {
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, this);

            ms.Seek(0, SeekOrigin.Begin);

            using var fs = File.Open(filename, FileMode.Create);
            ms.CopyTo(fs);
        }

        #endregion
    }

    /// <summary>
    /// Represents basic metadata for an App, as well as all requests required to download the app.
    /// </summary>
    [ProtoContract(SkipConstructor = true)]
    public sealed class AppQueuedRequests
    {
        [ProtoMember(1)]
        public string AppName { get; }

        [ProtoMember(2)]
        public uint AppId { get; }

        [ProtoMember(3)]
        public List<QueuedRequest> QueuedRequests;

        public long TotalBytes => QueuedRequests.Sum(e => e.CompressedLength);

        public AppQueuedRequests(string appName, uint appId, List<QueuedRequest> queuedRequests)
        {
            AppName = appName;
            AppId = appId;
            QueuedRequests = queuedRequests;
        }

        public string TotalBytesFormatted => ByteSize.FromBytes(TotalBytes).ToBinaryString();

        public int FileCount => QueuedRequests.Count;
        public string FormattedFileCount => FileCount.ToString("n0");

        public ByteSize AverageChunkSize => ByteSize.FromBytes((double)TotalBytes / (double)FileCount);
        public string AverageChunkSizeFormatted
        {
            get
            {
                if (AverageChunkSize < ByteSize.FromKibiBytes(256))
                {
                    return LightRed(AverageChunkSize.ToBinaryString());
                }
                if (AverageChunkSize > ByteSize.FromKibiBytes(896))
                {
                    return LightGreen(AverageChunkSize.ToBinaryString());
                }
                return AverageChunkSize.ToBinaryString();
            }
        }
    }

    /// <summary>
    /// This is required in order to be able to serialize/deserialize CDN servers from SteamKit.
    /// SteamKit's Server class is internal, and has no public constructor, which means that System.Text.Json source generators cannot serialize it.
    /// Instead, this class is used for serialization, and then AutoMapper is used to create a real Server object with reflection.
    /// </summary>
    [ProtoContract(SkipConstructor = true)]
    public sealed class CdnServerShim
    {
        [ProtoMember(1)]
        public string Host { get; set; }

        public override string ToString()
        {
            if (Host == null)
            {
                return "";
            }
            return Host;
        }
    }

    /// <summary>
    /// Defines available preset workloads, which consist of a single app.  These apps were chosen since they are free and are available by every account.
    /// Each app has differing performance characteristics:
    /// Destiny 2 is as close to an ideal workload as possible, as nearly all of its chunks are 1mb or larger.
    /// Dota 2 is a worst case workload, as the chunks are very small
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    [Intellenum(typeof(string))]
    public sealed partial class PresetWorkload
    {
        public static readonly PresetWorkload LargeChunks = new PresetWorkload("1085660");
        public static readonly PresetWorkload SmallChunks = new PresetWorkload("570");
    }
}