#region

using System;
using System.Collections.Generic;
using System.CommandLine.IO;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using CsxTools.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using RoslynPad.Editor;
using RoslynPad.Roslyn;
using RoslynPad.Themes;

#endregion

namespace CsxTools.Editor;

public partial class MainWindow : Window
{
    private static Theme? _theme;
    private static RoslynHost? _host;

    private bool _exited;
    private bool _isDirty;
    private IStorageFile? _currentFile;
    private string? _openedSource;

    public MainWindow()
    {
        InitializeComponent();
        InitializeHostAndTheme(string.Empty, string.Empty);
    }

    private async Task SetSource(string source)
    {
        if (_host == null || _theme == null) return;

        CodeEditor.Text = string.Empty;
        _openedSource = source;

        await CodeEditor.InitializeAsync(
            _host,
            new ThemeClassificationColors(_theme),
            Directory.GetCurrentDirectory(),
            source,
            SourceCodeKind.Script
        );
    }

    private async Task InitializeHostAndTheme(string fileName, string source)
    {
        var asmListPath = $"{fileName}.asmlist";
        var metas = new List<MetadataReference>();
        var privateCoreAssembly = Assembly.Load("System.Private.CoreLib");
        metas.Add(MetadataReference.CreateFromFile(privateCoreAssembly.Location));
        
        foreach (var meta in ScriptOptions.Default.MetadataReferences.ToList())
        {
            if (meta is not UnresolvedMetadataReference unresolvedMetadataReference)
                continue;

            var assembly = Assembly.Load(unresolvedMetadataReference.Reference);
            metas.Add(MetadataReference.CreateFromFile(assembly.Location));
        }
        
        if (File.Exists(asmListPath))
        {
            var assemblyPaths = await File.ReadAllLinesAsync(asmListPath);
            foreach (var assemblyPath in assemblyPaths)
            {
                if (string.IsNullOrWhiteSpace(assemblyPath)) continue;

                var assembly = Assembly.LoadFile(assemblyPath);
                metas.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }

        _host = new CustomRoslynHost<ExecCommand.Globals>(new[]
        {
            Assembly.Load("RoslynPad.Roslyn.Avalonia"),
            Assembly.Load("RoslynPad.Editor.Avalonia")
        }, RoslynHostReferences.NamespaceDefault.With(
            metas,
            typeNamespaceImports: new []{ typeof(ExecCommand.Globals) }
        ));

        if (_theme == null)
        {
            var isDark = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
            var reader = new VsCodeThemeReader();
            var themeType = isDark ? ThemeType.Dark : ThemeType.Light;
            var themeName = isDark ? "dark_vs.json" : "light_vs.json";
            var theme = await reader.ReadThemeAsync(Path.Combine(AppContext.BaseDirectory, "Themes", themeName),
                themeType);
            _theme = theme;
        }

        await SetSource(source);
        UpdateTitle();
    }

    private void UpdateTitle()
    {
        var baseText = "CsxEditor";
        var fileName = _currentFile?.Name;
        var dirtyMark = _isDirty ? "*" : string.Empty;

        if (string.IsNullOrWhiteSpace(fileName))
            Title = $"{baseText} [script.csx{dirtyMark}]";
        else
            Title = $"{baseText} [{fileName}{dirtyMark}]";
    }

    private async void Open_OnClick(object? sender, RoutedEventArgs e)
    {
        var directory = await StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents);
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                FilePickerFileTypes.Csx,
            },
            SuggestedStartLocation = directory,
            Title = "Open CSharp ScriptFile"
        });
        if (files.Count > 0)
        {
            var file = files.First();
            _currentFile = file;

            await using var stream = await _currentFile.OpenReadAsync();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            await InitializeHostAndTheme(_currentFile.Path.ToString(), await reader.ReadToEndAsync());

            _isDirty = false;
            UpdateTitle();
        }
    }

    private async void Save_OnClick(object? sender, RoutedEventArgs e)
    {
        await Save();
    }

    private async Task Save()
    {
        if (_currentFile == null)
            await SaveAs();
        else
        {
            await using var stream = await _currentFile.OpenWriteAsync();
            stream.Position = 0;

            await using var writer = new StreamWriter(stream, Encoding.UTF8);
            await writer.WriteAsync(CodeEditor.Text);
            await writer.FlushAsync();

            _isDirty = false;
            UpdateTitle();
        }
    }

    private async void SaveAs_OnClick(object? sender, RoutedEventArgs e)
    {
        await SaveAs();
    }

    private async Task SaveAs()
    {
        var directory = await StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents);
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            FileTypeChoices = new[]
            {
                FilePickerFileTypes.Csx,
            },
            SuggestedStartLocation = directory,
            Title = "Save As CSharp ScriptFile",
            ShowOverwritePrompt = true,
            SuggestedFileName = "Script"
        });
        if (file != null)
        {
            _currentFile = file;
            
            await using var stream = await file.OpenWriteAsync();
            stream.Position = 0;

            await using var writer = new StreamWriter(stream, Encoding.UTF8);
            await writer.WriteAsync(CodeEditor.Text);
            await writer.FlushAsync();

            _isDirty = false;
            UpdateTitle();
        }
    }

    private void Exit_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
    
    private async void RunScript_OnClick(object? sender, RoutedEventArgs e)
    {
        var fileName = _currentFile?.Path.LocalPath;
        if (string.IsNullOrWhiteSpace(fileName))
            return;
        
        if (_isDirty)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
            {
                ContentHeader = "Warning",
                ContentMessage = "Do you want to save the file?",
                ButtonDefinitions = ButtonEnum.YesNo,
                Icon = MsBox.Avalonia.Enums.Icon.Warning,
                ShowInCenter = true
            });
            var result = await box.ShowAsPopupAsync(this);
            if (result == ButtonResult.No)
                return;
        }
        
        await Save();
        await RunScript(fileName);
    }

    private async void RunScriptCustom_OnClick(object? sender, RoutedEventArgs e)
    {
        var fileName = _currentFile?.Path.LocalPath;
        if (string.IsNullOrWhiteSpace(fileName))
            return;

        if (_isDirty)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
            {
                ContentHeader = "Warning",
                ContentMessage = "Do you want to save the file?",
                ButtonDefinitions = ButtonEnum.YesNo,
                Icon = MsBox.Avalonia.Enums.Icon.Warning,
                ShowInCenter = true
            });
            var result = await box.ShowAsPopupAsync(this);
            if (result == ButtonResult.No)
                return;
        }

        fileName = Path.GetFullPath(fileName);
        
        var box2 = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
        {
            ContentHeader = "Input",
            ContentMessage = "Enter your custom arguments.",
            ButtonDefinitions = ButtonEnum.OkCancel,
            Icon = MsBox.Avalonia.Enums.Icon.Info,
            InputParams = new InputParams(),
            ShowInCenter = true
        });
        var result2 = await box2.ShowAsPopupAsync(this);
        if (result2 == ButtonResult.Cancel)
            return;
        
        await RunScript(fileName, box2.InputValue);
    }

    private async Task RunScript(string fileName, string? additionalArgs = null)
    {
        var args = new List<string>();
        args.Add("CsxTools.Editor");
        args.Add("exec");
        args.Add(fileName);
        
        if (!string.IsNullOrWhiteSpace(additionalArgs))
            args.AddRange(additionalArgs.Split(" ", StringSplitOptions.RemoveEmptyEntries));
        
        var testConsole = new TestConsole();
        await new CsxApplication().StartAsync(args.ToArray(), testConsole);

        var output = testConsole.Out.ToString();
        if (!string.IsNullOrWhiteSpace(output))
        {
            var box = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
            {
                ContentHeader = "Output",
                ContentMessage = output,
                ButtonDefinitions = ButtonEnum.Ok,
                Icon = MsBox.Avalonia.Enums.Icon.Info,
                ShowInCenter = true
            });
            await box.ShowAsPopupAsync(this);
        }

        var error = testConsole.Error.ToString();
        if (!string.IsNullOrWhiteSpace(error))
        {
            var box = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
            {
                ContentHeader = "Error",
                ContentMessage = error,
                ButtonDefinitions = ButtonEnum.Ok,
                Icon = MsBox.Avalonia.Enums.Icon.Error,
                ShowInCenter = true
            });
            await box.ShowAsPopupAsync(this);
        }
    }

    private async void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_exited) return;

        if (_isDirty)
        {
            e.Cancel = true;
            var box = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
            {
                ContentHeader = "Warning",
                ContentMessage = "Are you sure you want to close it?",
                ButtonDefinitions = ButtonEnum.YesNo,
                Icon = MsBox.Avalonia.Enums.Icon.Warning,
                ShowInCenter = true
            });
            var result = await box.ShowAsPopupAsync(this);
            _exited = result == ButtonResult.Yes;

            if (_exited) Close();
        }
    }

    private void CodeEditor_OnTextChanged(object? sender, EventArgs e)
    {
        if (_isDirty) return;

        _isDirty = _openedSource != CodeEditor.Text;

        if (_isDirty) UpdateTitle();
    }

    private async void About_OnClick(object? sender, RoutedEventArgs e)
    {
        var asm = Assembly.GetExecutingAssembly();
        var version = asm.GetName().Version;
        var box = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
        {
            ContentHeader = "About CsxEditor",
            ContentMessage = $"Version: {version.Major}.{version.Minor}.{version.Build}.{version.Revision}",
            ButtonDefinitions = ButtonEnum.Ok,
            Icon = MsBox.Avalonia.Enums.Icon.Info,
            ShowInCenter = true
        });
        await box.ShowAsPopupAsync(this);
    }
}