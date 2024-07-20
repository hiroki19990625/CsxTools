using Avalonia.Platform.Storage;

namespace CsxTools.Editor;

public class FilePickerFileTypes
{
    public static FilePickerFileType Csx { get; } = new("CSharp Script")
    {
        Patterns = new[] { "*.csx" },
        AppleUniformTypeIdentifiers = new[] { "com.microsoft.csharp.script" },
        MimeTypes = new[] { "application/csx" }
    };
}