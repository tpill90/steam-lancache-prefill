namespace SteamPrefill.Utils
{
    public static class ByteSizeExtensions
    {
        public static string ToDecimalString(this ref ByteSize byteSize)
        {
            return byteSize.ToString("0.##", CultureInfo.CurrentCulture, true);
        }

        public static string ToAverageString(this ref ByteSize byteSize, Stopwatch timer)
        {
            var averageSpeed = ByteSize.FromBytes(byteSize.Bytes / timer.Elapsed.TotalSeconds);

            var megabits = averageSpeed.MegaBytes * 8;
            if (megabits < 1000)
            {
                return $"{megabits.ToString("0.##")} Mbit/s";
            }

            return $"{(averageSpeed.GigaBytes * 8).ToString("0.##")} Gbit/s";
        }
    }
}