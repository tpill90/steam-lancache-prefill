namespace LancachePrefill.Common.Extensions
{
    public static class ByteSizeExtensions
    {
        public static string ToDecimalString(this ref ByteSize byteSize)
        {
            return byteSize.ToString("0.##", CultureInfo.CurrentCulture, true);
        }

        public static string CalculateBitrate(this ref ByteSize byteSize, Stopwatch timer)
        {
            return CalculateBitrate(ref byteSize, timer.Elapsed);
        }

        public static string CalculateBitrate(this ref ByteSize byteSize, TimeSpan elapsed)
        {
            var averageSpeed = ByteSize.FromBytes(byteSize.Bytes / elapsed.TotalSeconds);

            var megabits = averageSpeed.MegaBytes * 8;
            if (megabits < 1000)
            {
                return $"{megabits.ToString("0.##")} Mbit/s";
            }

            return $"{(averageSpeed.GigaBytes * 8).ToString("0.##")} Gbit/s";
        }
    }
}