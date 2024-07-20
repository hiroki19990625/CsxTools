using System.CommandLine;
using CsxTools.Commands;
using CsxTools.SignOnly.Commands;

namespace CsxTools.SignOnly;

public class SignOnlyCsxApplication : CsxApplication
{
    public override async Task StartAsync(string[] args)
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand( new VersionCommand());
        rootCommand.AddCommand( new SignOnlyExecCommand());
        rootCommand.AddCommand(new SignCommand());

        await rootCommand.InvokeAsync(args);
    }
}