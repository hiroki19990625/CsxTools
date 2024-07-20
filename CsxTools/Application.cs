using System.CommandLine;
using CsxTools.Commands;

namespace CsxTools;

public class Application
{
    public virtual async Task StartAsync(string[] args)
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand( new VersionCommand());
        rootCommand.AddCommand( new ExecCommand());
        rootCommand.AddCommand(new SignCommand());

        await rootCommand.InvokeAsync(args);
    }
}