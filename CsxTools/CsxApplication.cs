using System.CommandLine;
using System.CommandLine.IO;
using CsxTools.Commands;

namespace CsxTools;

public class CsxApplication
{
    public virtual async Task StartAsync(string[] args, IConsole? console = null)
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand( new VersionCommand());
        rootCommand.AddCommand( new ExecCommand());
        rootCommand.AddCommand(new SignCommand());

        await rootCommand.InvokeAsync(args, console);
    }
}