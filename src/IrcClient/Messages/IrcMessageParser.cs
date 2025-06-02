using System;
using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace IrcClient.Messages;

internal static class IrcMessageParser
{
    internal static RawIrcMessage? ParseMessage<TChar>(ReadOnlySpan<TChar> s, bool tryParse)
        where TChar : unmanaged, IBinaryInteger<TChar>
    {
        if (!ParseTags(ref s, tryParse, out var tags)) return null;
        if (!ParseSourceWithinMessage(ref s, tryParse, out var source)) return null;
        if (!ParseCommand(ref s, tryParse, out var command)) return null;
        if (!ParseParameters(ref s, tryParse, out var parameters)) return null;

        return new RawIrcMessage
        {
            Tags = tags,
            Source = source,
            Command = command,
            Parameters = parameters,
        };
    }

    private static bool ParseTags<TChar>(ref ReadOnlySpan<TChar> s, bool tryParse, [NotNullWhen(true)] out ImmutableDictionary<string, string?>? tags)
        where TChar : unmanaged, IBinaryInteger<TChar>
    {
        if (!s.StartsWith(TChar.CreateTruncating('@')))
        {
            tags = ImmutableDictionary<string, string?>.Empty;
            return true;
        }

        tags = null;
        s = s[1..];

        ReadOnlySpan<TChar> tagsSpan;
        switch (s.SplitOnceConsecutive(TChar.CreateTruncating(' ')))
        {
            case ([], _):
                if (!tryParse) ThrowFormatException("The tag section must not be empty");
                return false;
            case (_, []):
                if (!tryParse) ThrowFormatException("Incomplete message");
                return false;
            case (var span, var rest):
                tagsSpan = span;
                s = rest;
                break;
        }

        var tagBuilder = ImmutableDictionary.CreateBuilder<string, string?>();
        var valueBuffer = new ArrayBufferWriter<char>(16);
        var decoder = Encoding.UTF8.GetDecoder();
        foreach (var range in tagsSpan.Split(TChar.CreateTruncating(';')))
        {
            var tag = tagsSpan[range];
            if (tag.IsEmpty)
            {
                if (!tryParse) ThrowFormatException("Empty tag found");
                return false;
            }

            switch (tag.SplitOnce(TChar.CreateTruncating('=')))
            {
                case ([], _):
                    if (!tryParse) ThrowFormatException("Empty tag key found");
                    return false;
                case (var key, []):
                    tagBuilder.Add(ToString(key), null);
                    break;
                case (var key, var value):
                    tagBuilder[ToString(key)] = ParseTagValue(value, valueBuffer, decoder);
                    valueBuffer.ResetWrittenCount();
                    decoder.Reset();
                    break;
            }
        }

        tags = tagBuilder.ToImmutable();
        return true;
    }

    private static string ParseTagValue<TChar>(ReadOnlySpan<TChar> s, ArrayBufferWriter<char> buffer, Decoder decoder)
        where TChar : unmanaged, IBinaryInteger<TChar>
    {
        while (true)
        {
            (var unescaped, s) = s.SplitOnce(TChar.CreateTruncating('\\'));
            if (!unescaped.IsEmpty) ToString(unescaped, buffer, decoder);
            if (s.IsEmpty) break;

            if (s[0] == TChar.CreateTruncating(':'))
            {
                buffer.Write([';']);
                s = s[1..];
            }
            else if (s[0] == TChar.CreateTruncating('s'))
            {
                buffer.Write([' ']);
                s = s[1..];
            }
            else if (s[0] == TChar.CreateTruncating('\\'))
            {
                buffer.Write(['\\']);
                s = s[1..];
            }
            else if (s[0] == TChar.CreateTruncating('r'))
            {
                buffer.Write(['\r']);
                s = s[1..];
            }
            else if (s[0] == TChar.CreateTruncating('n'))
            {
                buffer.Write(['\n']);
                s = s[1..];
            }
            // \ followed by an invalid character has the \ dropped, so \b unescapes to b.
            // So don't skip so the next loop iteration picks up the character.
        }

        return buffer.WrittenSpan.ToString();
    }

    private static bool ParseSourceWithinMessage<TChar>(ref ReadOnlySpan<TChar> s, bool tryParse, out IrcMessageSource? source)
        where TChar : unmanaged, IBinaryInteger<TChar>
    {
        source = null;
        if (!s.StartsWith(TChar.CreateTruncating(':')))
        {
            return true;
        }

        s = s[1..];

        ReadOnlySpan<TChar> sourceSpan;
        switch (s.SplitOnceConsecutive(TChar.CreateTruncating(' ')))
        {
            case ([], _):
                if (!tryParse) ThrowFormatException("The source section must not be empty");
                return false;
            case (_, []):
                if (!tryParse) ThrowFormatException("Incomplete message");
                return false;
            case (var span, var rest):
                sourceSpan = span;
                s = rest;
                break;
        }

        source = ParseSource(sourceSpan);
        return true;
    }

    internal static IrcMessageSource ParseSource<TChar>(ReadOnlySpan<TChar> s)
        where TChar : unmanaged, IBinaryInteger<TChar>
    {
        (var remainder, var host) = s.SplitOnce(TChar.CreateTruncating('@'));
        (var serverOrNick, var user) = remainder.SplitOnce(TChar.CreateTruncating('!'));

        return new IrcMessageSource
        {
            ServerOrNick = ToString(serverOrNick),
            User = user.IsEmpty ? null : ToString(user),
            Host = host.IsEmpty ? null : ToString(host),
        };
    }

    private static bool ParseCommand<TChar>(ref ReadOnlySpan<TChar> s, bool tryParse, [NotNullWhen(true)] out string? command)
        where TChar : unmanaged, IBinaryInteger<TChar>
    {
        (var commandSpan, s) = s.SplitOnceConsecutive(TChar.CreateTruncating(' '));
        if (commandSpan.IsEmpty)
        {
            if (!tryParse) ThrowFormatException("No command found in the message");
            command = null;
            return false;
        }

        command = ToString(commandSpan);
        return true;
    }

    private static bool ParseParameters<TChar>(ref ReadOnlySpan<TChar> s, bool tryParse, out ImmutableArray<string> parameters)
        where TChar : unmanaged, IBinaryInteger<TChar>
    {
        if (s.IsEmpty)
        {
            parameters = ImmutableArray<string>.Empty;
            return true;
        }

        var paramsBuilder = ImmutableArray.CreateBuilder<string>();
        while (!s.IsEmpty)
        {
            if (s.StartsWith(TChar.CreateTruncating(':')))
            {
                paramsBuilder.Add(ToString(s[1..]));
                s = ReadOnlySpan<TChar>.Empty;
                break;
            }

            (var param, s) = s.SplitOnceConsecutive(TChar.CreateTruncating(' '));
            paramsBuilder.Add(ToString(param));
        }

        parameters = paramsBuilder.DrainToImmutable();
        return true;
    }

    private static string ToString<TChar>(ReadOnlySpan<TChar> s)
        where TChar : unmanaged, IBinaryInteger<TChar>
    {
        if (typeof(TChar) == typeof(char))
        {
            return s.ToString();
        }

        if (typeof(TChar) == typeof(byte))
        {
            return Encoding.UTF8.GetString(MemoryMarshal.Cast<TChar, byte>(s));
        }

        ThrowToStringUnreachable();
        return null!;
    }

    private static void ToString<TChar>(ReadOnlySpan<TChar> s, ArrayBufferWriter<char> buffer, Decoder decoder)
        where TChar : unmanaged, IBinaryInteger<TChar>
    {
        if (typeof(TChar) == typeof(char))
        {
            var span = MemoryMarshal.Cast<TChar, char>(s);
            buffer.Write(span);
            return;
        }

        if (typeof(TChar) == typeof(byte))
        {
            var span = MemoryMarshal.Cast<TChar, byte>(s);
            decoder.Convert(span, buffer, true, out _, out _);
            return;
        }
        
        ThrowToStringUnreachable();
    }

    [DoesNotReturn]
    private static void ThrowToStringUnreachable() => throw new UnreachableException("Can only ToString spans of char or byte");

    [DoesNotReturn]
    private static void ThrowFormatException(string message) => throw new FormatException(message);
}
