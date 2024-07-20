using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace CsxTools.Core;

public class ScriptContext
{
    public Script<object> CreateScript<TGlobal>(string code, string[] assemblyFiles, bool isSandbox)
    {
        var assemblyLoader = new InteractiveAssemblyLoader();
        var assemblyMetas = new List<MetadataReference>();
        var option = ScriptOptions.Default;
        if (!isSandbox)
            assemblyMetas = ScriptOptions.Default.MetadataReferences.ToList();

        foreach (var assemblyFile in assemblyFiles)
        {
            var assembly = Assembly.Load(assemblyFile);
            assemblyMetas.Add(MetadataReference.CreateFromFile(assembly.Location));
            assemblyLoader.RegisterDependency(assembly);
        }
        
        option = option.WithReferences(assemblyMetas);
        return CSharpScript.Create(code, option, typeof(TGlobal), assemblyLoader);
    }
}