#region

using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Invocation;

#endregion

namespace CsxTools.Commands;

public static class CommandExtensions
{
    public static T? GetValueForHandlerParameter<T>(
        this IValueDescriptor<T> symbol,
        InvocationContext context)
    {
        if (symbol is IValueSource valueSource &&
            valueSource.TryGetValue(symbol, context.BindingContext, out var boundValue) &&
            boundValue is T value)
        {
            return value;
        }
        else
        {
            return symbol switch
            {
                Argument<T> argument => context.ParseResult.GetValueForArgument(argument),
                Option<T> option => context.ParseResult.GetValueForOption(option),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}