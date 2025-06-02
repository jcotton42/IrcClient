using System;
using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace IrcClient.Messages;

internal static class IrcMessageFormatter
{
    private static readonly SearchValues<char> TagValueEscapes = SearchValues.Create("; \\\r\n");

    internal static bool TryFormat<TChar>(RawIrcMessage message, Span<TChar> destination, out int countWritten)
        where TChar : unmanaged, IBinaryInteger<TChar>
    {
        countWritten = 0;
        if (!TryFormatTags(message.Tags, destination, ref countWritten)) return false;
        if (!TryFormatSourceWithinMessage(message.Source, destination, ref countWritten)) return false;
        if (!TryFormatCommand(message.Command, destination, ref countWritten)) return false;
        if (!TryFormatParameters(message.Parameters, destination, ref countWritten)) return false;

        return true;
    }

    private static bool TryFormatTags<TChar>(ImmutableDictionary<string, string?> tags, Span<TChar> destination, ref int countWritten)
        where TChar : unmanaged, IBinaryInteger<TChar>
    {
        if (tags.IsEmpty) return true;
        if (!TryPut(destination[countWritten..], '@')) return false;
        countWritten++;

        var first = true;
        foreach (var (key, value) in tags)
        {
            if (!first)
            {
                if (!TryPut(destination[countWritten..], ';')) return false;
                countWritten++;
            }

            if (!TryCopyTo(key, destination[countWritten..], ref countWritten)) return false;
            if (!TryFormatTagValue(value, destination, ref countWritten)) return false;
            first = false;
        }

        if (!TryPut(destination[countWritten..], ' ')) return false;
        countWritten++;

        return true;
    }

    private static bool TryFormatTagValue<TChar>(string? value, Span<TChar> destination, ref int countWritten) where TChar : unmanaged, IBinaryInteger<TChar>
    {
        if (value is null or "") return true;

        if (!TryPut(destination[countWritten..], '=')) return false;
        countWritten++;

        var v = value.AsSpan();
        while (!v.IsEmpty)
        {
            var escapeIndex = v.IndexOfAny(TagValueEscapes);
            ReadOnlySpan<char> unescaped;
            if (escapeIndex < 0)
            {
                unescaped = v;
                if (!TryCopyTo(unescaped, destination[countWritten..], ref countWritten)) return false;
                break;
            }

            unescaped = v[..escapeIndex];
            var ch = v[escapeIndex] switch
            {
                ';' => ':',
                ' ' => 's',
                '\\' => '\\',
                '\r' => 'r',
                '\n' => 'n',
                _ => ThrowUnreachable(),
            };
            v = v[(escapeIndex + 1)..];

            if (!TryCopyTo(unescaped, destination[countWritten..], ref countWritten)) return false;

            if (!TryPut(destination[countWritten..], '\\')) return false;
            countWritten++;

            if (!TryPut(destination[countWritten..], ch)) return false;
            countWritten++;
        }

        return true;

        [DoesNotReturn]
        char ThrowUnreachable() => throw new UnreachableException("TryFormatValueTag's SearchValues need to be updated");
    }
    
    private static bool TryFormatSourceWithinMessage<TChar>(IrcMessageSource? source, Span<TChar> destination, ref int countWritten)
        where TChar : unmanaged, IBinaryInteger<TChar>
    {
        if (source is null) return true;

        if (!TryPut(destination[countWritten..], ':')) return false;
        countWritten++;

        if (!TryFormatSource(source, destination, ref countWritten)) return false;

        if (!TryPut(destination[countWritten..], ' ')) return false;
        countWritten++;

        return true;
    }

    internal static bool TryFormatSource<TChar>(IrcMessageSource source, Span<TChar> destination, ref int countWritten)
        where TChar : unmanaged, IBinaryInteger<TChar>
    {
        if (!TryCopyTo(source.ServerOrNick, destination[countWritten..], ref countWritten)) return false;

        if (source.User is not null)
        {
            if (!TryPut(destination[countWritten..], '!')) return false;
            countWritten++;

            if (!TryCopyTo(source.User, destination[countWritten..], ref countWritten)) return false;
        }

        if (source.Host is not null)
        {
            if (!TryPut(destination[countWritten..], '@')) return false;
            countWritten++;

            if (!TryCopyTo(source.Host, destination[countWritten..], ref countWritten)) return false;
        }

        return true;
    }

    private static bool TryFormatCommand<TChar>(string command, Span<TChar> destination, ref int countWritten)
        where TChar : unmanaged, IBinaryInteger<TChar>
    {
        return TryCopyTo(command, destination[countWritten..], ref countWritten);
    }

    private static bool TryFormatParameters<TChar>(ImmutableArray<string> parameters, Span<TChar> destination, ref int countWritten)
        where TChar : unmanaged, IBinaryInteger<TChar>
    {
        if (parameters.IsEmpty) return true;

        for (var i = 0; i < parameters.Length - 1; i++)
        {
            if (!TryPut(destination[countWritten..], ' ')) return false;
            countWritten++;

            if (!TryCopyTo(parameters[i], destination[countWritten..], ref countWritten)) return false;
        }

        if (!TryCopyTo(" :", destination[countWritten..], ref countWritten)) return false;
        if (!TryCopyTo(parameters[^1], destination[countWritten..], ref countWritten)) return false;

        return true;
    }

    private static bool TryPut<TChar>(Span<TChar> destination, char value)
        where TChar : unmanaged, IBinaryInteger<TChar>
    {
        if (destination.IsEmpty) return false;
        destination[0] = TChar.CreateTruncating(value);
        return true;
    }

    private static bool TryCopyTo<TChar>(ReadOnlySpan<char> source, Span<TChar> destination, ref int countWritten)
        where TChar : unmanaged, IBinaryInteger<TChar>
    {
        if (source.IsEmpty) return true;
        if (typeof(TChar) == typeof(char))
        {
            if (!source.TryCopyTo(MemoryMarshal.Cast<TChar, char>(destination))) return false;
            countWritten += source.Length;
            return true;
        }

        if (typeof(TChar) == typeof(byte))
        {
            if (!Encoding.UTF8.TryGetBytes(source, MemoryMarshal.Cast<TChar, byte>(destination), out var written)) return false;
            countWritten += written;
            return true;
        }

        return ThrowUnreachableCopyTo();

        [DoesNotReturn]
        bool ThrowUnreachableCopyTo() => throw new UnreachableException("TryCopyTo can only be used for char and byte");
    }
}
