using System;
using System.Diagnostics;
using Spectre.Console;

namespace DepotDownloader.Utils
{
    public sealed class AutoTimer : IDisposable
    {
        private readonly string _message;
        private readonly IAnsiConsole _ansiConsole;
        private readonly Stopwatch _stopwatch;

        public AutoTimer(IAnsiConsole ansiConsole, string message)
        {
            _message = message;
            _ansiConsole = ansiConsole;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _ansiConsole.LogMarkupLine(_message, _stopwatch.Elapsed);
        }
    }
}
