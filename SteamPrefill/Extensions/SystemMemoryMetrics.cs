namespace SteamPrefill.Extensions
{
    public static class SystemMemoryMetrics
    {
        public static ByteSize GetTotalSystemMemory()
        {
            var info = new ProcessStartInfo("free -m")
            {
                FileName = "/bin/bash",
                Arguments = "-c \"free -m\"",
                RedirectStandardOutput = true
            };

            using var process = Process.Start(info);
            var output = process.StandardOutput.ReadToEnd();

            var lines = output.Split("\n");
            var memory = lines[1].Split(" ", StringSplitOptions.RemoveEmptyEntries);

            return ByteSize.FromBytes(double.Parse(memory[1]) * ByteSize.FromMebiBytes(1).Bytes);
        }
    }
}