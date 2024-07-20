#region

using System.CommandLine;

#endregion

namespace CsxTools.Core;

public class CommandConsole(IConsole console) : ICommandConsole
{
    public void Write(string text)
    {
        console.Write(text);
    }

    public void WriteLine(string text)
    {
        console.WriteLine(text);
    }
}