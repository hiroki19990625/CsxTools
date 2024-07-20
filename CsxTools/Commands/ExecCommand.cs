using System.CommandLine;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using CsxTools.Core;

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
        
        this.SetHandler(async (fileName, args, isSandbox, certFile) =>
        {
            if (!File.Exists(fileName))
            {
                Console.WriteLine($"File not found. ({fileName})");
                return;
            }

            if (Path.GetExtension(fileName) != ".csx")
            {
                Console.WriteLine($"File extension not support. ({fileName})");
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
                var context = new ScriptContext();
                var script = context.CreateScript<Globals>(source, assemblyFiles, isSandbox);
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
                
                await runner.Invoke(new Globals(args));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }, fileNameArgs, argsOption, isSandboxOption, certFileOption);
    }

    public class Globals(string[] args)
    {
        public string[] args { get; } = args;
    }
}