using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Unicode;

namespace IrcClient.Messages;

public sealed class RawIrcMessage : ISpanParsable<RawIrcMessage>, IUtf8SpanParsable<RawIrcMessage>
{
    public required ImmutableDictionary<string, string?> Tags { get; init; }
    public required IrcMessageSource? Source { get; init; }
    public required string Command { get; init; }
    public required ImmutableArray<string> Parameters { get; init; }

    public static RawIrcMessage Parse(string s) => Parse(s.AsSpan(), null);

    public static RawIrcMessage Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);

    public static bool TryParse([NotNullWhen(true)] string? s, [MaybeNullWhen(false)] out RawIrcMessage result) =>
        TryParse(s.AsSpan(), null, out result);

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider,
        [MaybeNullWhen(false)] out RawIrcMessage result) =>
        TryParse(s.AsSpan(), provider, out result);

    public static RawIrcMessage Parse(ReadOnlySpan<char> s) => Parse(s, null);

    public static bool TryParse(ReadOnlySpan<char> s, [MaybeNullWhen(false)] out RawIrcMessage result) =>
        TryParse(s, null, out result);

    public static RawIrcMessage Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        return IrcMessageParser.ParseMessage(s, tryParse: false)!;
    }

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out RawIrcMessage result)
    {
        result = IrcMessageParser.ParseMessage(s, tryParse: true);
        return result is not null;
    }

    public static RawIrcMessage Parse(ReadOnlySpan<byte> utf8Text) => Parse(utf8Text, null);
    public static RawIrcMessage Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
    {
        return IrcMessageParser.ParseMessage(utf8Text, tryParse: false)!;
    }

    public static bool TryParse(ReadOnlySpan<byte> utf8Text, [MaybeNullWhen(false)] out RawIrcMessage result) =>
        TryParse(utf8Text, null, out result);
    public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [MaybeNullWhen(false)] out RawIrcMessage result)
    {
        result = IrcMessageParser.ParseMessage(utf8Text, tryParse: true);
        return result is not null;
    }

    public override string ToString()
    {
        var builder = new StringBuilder();

        if (Tags.Count > 0)
        {
            builder.Append('@');
            var first = true;
            foreach (var (key, value) in Tags)
            {
                if (!first)
                {
                    builder.Append(';');
                }

                builder.Append(key);
                if (value is not (null or ""))
                {
                    builder.Append('=');
                    foreach (var c in value)
                    {
                        builder.Append(c switch
                        {
                            ';' => @"\:",
                            ' ' => @"\s",
                            '\\' => @"\\",
                            '\r' => @"\r",
                            '\n' => @"\n",
                            _ => [c],
                        });
                    }
                }
                first = false;
            }

            builder.Append(' ');
        }

        if (Source is not null)
        {
            builder.Append($":{Source} ");
        }

        builder.Append(Command);

        if (!Parameters.IsEmpty)
        {
            for (var i = 0; i < Parameters.Length - 1; i++)
            {
                builder.Append($" {Parameters[i]}");
            }

            builder.Append($" :{Parameters[^1]}");
        }

        return builder.ToString();
    }
}

public sealed class IrcMessageSource : ISpanParsable<IrcMessageSource>, IUtf8SpanParsable<IrcMessageSource>, ISpanFormattable, IUtf8SpanFormattable
{
    public required string ServerOrNick { get; init; }
    public string? User { get; init; }
    public string? Host { get;init; }

    public static IrcMessageSource Parse(string s) => Parse(s.AsSpan(), null);
    public static IrcMessageSource Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);

    public static bool TryParse([NotNullWhen(true)] string? s, [MaybeNullWhen(false)] out IrcMessageSource result) =>
        TryParse(s.AsSpan(), null, out result);
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider,
        [MaybeNullWhen(false)] out IrcMessageSource result) =>
        TryParse(s.AsSpan(), provider, out result);

    public static IrcMessageSource Parse(ReadOnlySpan<char> s) => Parse(s, null);
    public static IrcMessageSource Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
        IrcMessageParser.ParseSource(s);

    public static bool TryParse(ReadOnlySpan<char> s, [MaybeNullWhen(false)] out IrcMessageSource result) =>
        TryParse(s, null, out result);
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out IrcMessageSource result)
    {
        result = IrcMessageParser.ParseSource(s);
        return true;
    }

    public static IrcMessageSource Parse(ReadOnlySpan<byte> utf8Text) => Parse(utf8Text, null);
    public static IrcMessageSource Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
    {
        return IrcMessageParser.ParseSource(utf8Text);
    }

    public static bool TryParse(ReadOnlySpan<byte> utf8Text, [MaybeNullWhen(false)] out IrcMessageSource result) =>
        TryParse(utf8Text, null, out result);
    public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [MaybeNullWhen(false)] out IrcMessageSource result)
    {
        result = IrcMessageParser.ParseSource(utf8Text);
        return true;
    }

    public override string ToString() => ToString(null, null);
    public string ToString(string? format, IFormatProvider? formatProvider) => $"{this}";

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (!ServerOrNick.TryCopyTo(destination))
        {
            charsWritten = 0;
            return false;
        }

        charsWritten = ServerOrNick.Length;
        if (User is not null)
        {
            if (!destination[charsWritten..].TryWrite($"!{User}", out var written))
            {
                return false;
            }

            charsWritten += written;
        }

        if (Host is not null)
        {
            if (!destination[charsWritten..].TryWrite($"@{Host}", out var written))
            {
                return false;
            }

            charsWritten += written;
        }

        return true;
    }

    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (!Encoding.UTF8.TryGetBytes(ServerOrNick, utf8Destination, out bytesWritten))
        {
            return false;
        }

        if (User is not null)
        {
            if (!Utf8.TryWrite(utf8Destination, $"!{User}", out var written))
            {
                return false;
            }

            bytesWritten += written;
        }

        if (Host is not null)
        {
            if (!Utf8.TryWrite(utf8Destination, $"@{Host}", out var written))
            {
                return false;
            }

            bytesWritten += written;
        }

        return true;
    }
}
