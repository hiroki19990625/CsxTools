using System.CommandLine;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace CsxTools.Commands;

public class SignCommand : Command
{
    public static string NAME => "sign";
    
    public SignCommand() : base(NAME, string.Empty)
    {
        var fileNameArgs = new Argument<string>
        {
            Name = "fileName"
        };
        var certFileOption = new Option<string>("--certFile")
        {
            IsRequired = true,
            Arity = ArgumentArity.ExactlyOne
        };
        AddArgument(fileNameArgs);
        AddOption(certFileOption);
        
        this.SetHandler((fileName, certFile) =>
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
            var assemblyFiles = string.Empty;
            if (File.Exists(asmListFilePath))
            {
                assemblyFiles = File.ReadAllText(asmListFilePath);
            }

            if (!File.Exists(certFile))
                throw new SecurityException("Require cert file.");
            
            var source = File.ReadAllText(fileName);

            var ecDsa = ECDsa.Create();
            ecDsa.ImportFromPem(File.ReadAllText(certFile));
            var sourceBuffer = Encoding.Default.GetBytes(source + assemblyFiles);
            var signBuffer = ecDsa.SignData(sourceBuffer, HashAlgorithmName.SHA256);
            
            File.WriteAllText($"{fileName}.sign", Convert.ToHexString(signBuffer));
        }, fileNameArgs, certFileOption);
    }
}