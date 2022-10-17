// ReSharper disable MemberCanBePrivate.Global - Properties used as parameters can't be private with CliFx, otherwise they won't work.
namespace SteamPrefill.CliCommands.Benchmark
{
    [UsedImplicitly]
    [Command("benchmark", Description = "Used to configure and run a preset benchmark workload.")]
    public class BenchmarkCommand : ICommand
    {
        public async ValueTask ExecuteAsync(IConsole console)
        {
            // Required in order to have the method be async
            await Task.CompletedTask;

            throw new CommandException("'benchmark' requires a sub-command.  See help output for available sub-commands", 133, true);
        }
    }
}