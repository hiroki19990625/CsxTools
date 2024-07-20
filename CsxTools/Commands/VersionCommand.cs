using System.CommandLine;
using System.Reflection;

namespace CsxTools.Commands;

public class VersionCommand : Command
{
    public static string NAME => "version";
    
    public VersionCommand() : base(NAME, string.Empty)
    {
        this.SetHandler(() =>
        {
            var asm = Assembly.GetExecutingAssembly();
            var version = asm.GetName().Version;
            Console.WriteLine($"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}");
        });
    }
}