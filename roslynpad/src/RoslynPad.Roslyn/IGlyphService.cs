using RoslynPad.Roslyn.Completion;

namespace RoslynPad.Roslyn;

public interface IGlyphService
{
    object? GetGlyphImage(Glyph glyph);
}
