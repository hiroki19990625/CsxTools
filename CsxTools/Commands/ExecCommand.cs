#region

using System.CommandLine;
using System.CommandLine.Invocation;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using CsxTools.Core;

#endregion

namespace CsxTools.Commands;

public class ExecCommand : Command
{
    public static string NAME => "exec";

    public ExecCommand() : base(NAME, string.Empty)
    {
        var fileNameArgs = new Argument<string>
        {
            Name = "fileName"
        };
        var argsOption = new Option<string[]>("--args", () => [])
        {
            AllowMultipleArgumentsPerToken = true,
            Arity = ArgumentArity.OneOrMore
        };
        var isSandboxOption = new Option<bool>("--sandbox")
        {
            Arity = ArgumentArity.Zero
        };
        var certFileOption = new Option<string>("--certFile")
        {
            Arity = ArgumentArity.ExactlyOne
        };
        AddArgument(fileNameArgs);
        AddOption(argsOption);
        AddOption(isSandboxOption);
        AddOption(certFileOption);

        async Task OnHandler(InvocationContext context)
        {
            var fileName = fileNameArgs.GetValueForHandlerParameter(context);
            var args = argsOption.GetValueForHandlerParameter(context);
            var isSandbox = isSandboxOption.GetValueForHandlerParameter(context);
            var certFile = certFileOption.GetValueForHandlerParameter(context);

            if (!File.Exists(fileName))
            {
                context.Console.WriteLine($"File not found. ({fileName})");
                return;
            }

            if (Path.GetExtension(fileName) != ".csx")
            {
                context.Console.WriteLine($"File extension not support. ({fileName})");
                return;
            }

            var asmListFilePath = $"{fileName}.asmlist";
            string[] assemblyFiles = [];
            if (File.Exists(asmListFilePath))
            {
                assemblyFiles = File.ReadAllLines(asmListFilePath);
            }

            try
            {
                var source = File.ReadAllText(fileName);
                var scriptContext = new ScriptContext();
                var script = scriptContext.CreateScript<Globals>(source, assemblyFiles, isSandbox);
                var runner = script.CreateDelegate();
                var signatureFile = $"{fileName}.sign";
                if (File.Exists(signatureFile))
                {
                    if (!File.Exists(certFile))
                        throw new SecurityException("Require cert file.");

                    var ecDsa = ECDsa.Create();
                    ecDsa.ImportFromPem(File.ReadAllText(certFile));
                    var sourceBuffer = Encoding.Default.GetBytes(source);
                    var signBuffer = Convert.FromHexString(File.ReadAllText(signatureFile));
                    if (!ecDsa.VerifyData(sourceBuffer, signBuffer, HashAlgorithmName.SHA256))
                        throw new SecurityException("Cert verify fail.");
                }

                await runner.Invoke(new Globals(args, new CommandConsole(context.Console)));
            }
            catch (Exception e)
            {
                context.Console.WriteLine(e.ToString());
            }
        }

        this.SetHandler(OnHandler);
    }

    public class Globals(string[] args, ICommandConsole console)
    {
        public string[] args { get; } = args;
        public ICommandConsole console { get; } = console;
    }
}