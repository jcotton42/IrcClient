using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Unicode;

namespace IrcClient.Messages;

public sealed class RawIrcMessage : ISpanParsable<RawIrcMessage>, IUtf8SpanParsable<RawIrcMessage>, ISpanFormattable, IUtf8SpanFormattable
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

    public override string ToString() => ToString(null, null);
    public string ToString(string? format, IFormatProvider? formatProvider) => $"{this}";

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => 
        IrcMessageFormatter.TryFormat(this, destination, out charsWritten);

    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        IrcMessageFormatter.TryFormat(this, utf8Destination, out bytesWritten);
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
        charsWritten = 0;
        return IrcMessageFormatter.TryFormatSource(this, destination, ref charsWritten);
    }

    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        bytesWritten = 0;
        return IrcMessageFormatter.TryFormatSource(this, utf8Destination, ref bytesWritten);
    }
}
