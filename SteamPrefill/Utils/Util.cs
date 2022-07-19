using System.Runtime.InteropServices;

namespace SteamPrefill.Utils;

public static class OperatingSystem
{
    public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
}