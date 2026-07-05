// ReSharper disable MemberCanBePrivate.Global - CommandOption properties can't ever be private, otherwise they won't work with CliFx.
// ReSharper disable UnusedAutoPropertyAccessor.Global - Init setters are used even if resharper thinks they aren't, since CliFx sets them at runtime.
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