﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoslynPad.Roslyn;

namespace CsxTools.Editor;

public class CustomRoslynHost<T>(
    IEnumerable<Assembly>? additionalAssemblies = null,
    RoslynHostReferences? references = null,
    ImmutableArray<string>? disabledDiagnostics = null,
    ImmutableArray<string>? analyzerConfigFiles = null)
    : RoslynHost(additionalAssemblies, references, disabledDiagnostics, analyzerConfigFiles)
{
    protected override Project CreateProject(Solution solution, DocumentCreationArgs args, CompilationOptions compilationOptions,
        Project? previousProject = null)
    {
        var name = args.Name ?? "New";
        var path = Path.Combine(args.WorkingDirectory, name);
        var id = ProjectId.CreateNewId(name);

        var parseOptions = ParseOptions.WithKind(args.SourceCodeKind);
        var isScript = args.SourceCodeKind == SourceCodeKind.Script;

        if (isScript)
        {
            compilationOptions = compilationOptions.WithScriptClassName(name);
        }

        var analyzerConfigDocuments = AnalyzerConfigFiles.Where(File.Exists).Select(file => DocumentInfo.Create(
            DocumentId.CreateNewId(id, debugName: file),
            name: file,
            loader: new FileTextLoader(file, defaultEncoding: null),
            filePath: file));

        solution = solution.AddProject(ProjectInfo.Create(
                id,
                VersionStamp.Create(),
                name,
                name,
                LanguageNames.CSharp,
                filePath: path,
                isSubmission: isScript,
                parseOptions: parseOptions,
                hostObjectType: typeof(T),
                compilationOptions: compilationOptions,
                metadataReferences: previousProject != null ? [] : DefaultReferences,
                projectReferences: previousProject != null ? new[] { new ProjectReference(previousProject.Id) } : null)
            .WithAnalyzerConfigDocuments(analyzerConfigDocuments));

        var project = solution.GetProject(id)!;

        if (!isScript && GetUsings(project) is { Length: > 0 } usings)
        {
            project = project.AddDocument("RoslynPadGeneratedUsings", usings).Project;
        }

        return project;

        static string GetUsings(Project project)
        {
            if (project.CompilationOptions is CSharpCompilationOptions options)
            {
                return string.Join(" ", options.Usings.Select(i => $"global using {i};"));
            }

            return string.Empty;
        }
    }
}