using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace IrcClient.Messages;

public sealed class RawIrcMessage : ISpanParsable<RawIrcMessage>
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
        if (!TryParseCore(s, out var result))
        {
            throw new FormatException("Message was not well-formed.");
        }

        return result;
    }

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out RawIrcMessage result)
    {
        return TryParseCore(s, out result);
    }

    private static bool TryParseCore(ReadOnlySpan<char> s, [MaybeNullWhen(false)] out RawIrcMessage result)
    {
        result = null;
        // TODO pass up error info, probably will just `out` whatever error thing there is
        if (!TryParseTags(ref s, out var tags)) return false;
        if (!TryParseSource(ref s, out var source)) return false;
        if (!TryParseCommand(ref s, out var command)) return false;
        if (!TryParseParameters(ref s, out var parameters)) return false;

        result = new RawIrcMessage
        {
            Tags = tags,
            Source = source,
            Command = command,
            Parameters = parameters,
        };
        return true;
    }

    private static bool TryParseTags(ref ReadOnlySpan<char> s, out ImmutableDictionary<string, string?> result)
    {
        result = ImmutableDictionary<string, string?>.Empty;
        if (s[0] is not '@')
        {
            return true;
        }

        var spaceIndex = s.IndexOf(' ');
        if (spaceIndex < 0)
        {
            // TODO error for no end to tags
        }

        var tagsSpan = s[1..spaceIndex];
        s = s[spaceIndex..].SkipAll(' ');
        var tags = ImmutableDictionary.CreateBuilder<string, string?>();

        var builder = new StringBuilder();
        foreach (var range in tagsSpan.Split(';'))
        {
            var tag = tagsSpan[range];
            if (tag.IsEmpty)
            {
                // TODO error for empty tag
                return false;
            }

            var equalIndex = tag.IndexOf('=');
            if (equalIndex == 0)
            {
                // TODO error for empty tag name
                return false;
            }

            if (equalIndex < 0)
            {
                tags[tag.ToString()] = null;
                continue;
            }

            if (equalIndex == tag.Length - 1)
            {
                tags[tag[..equalIndex].ToString()] = null;
                continue;
            }

            tags[tag[..equalIndex].ToString()] = ParseTagValue(tag[(equalIndex + 1)..], builder);
        }

        result = tags.ToImmutable();
        return true;
    }

    private static string ParseTagValue(ReadOnlySpan<char> s, StringBuilder builder)
    {
        builder.Clear();
        var escape = false;

        foreach (var c in s)
        {
            if (escape)
            {
                builder.Append(c switch
                {
                    ':' => ';',
                    's' => ' ',
                    '\\' => '\\',
                    'r' => '\r',
                    'n' => '\n',
                    _ => c,
                });
                escape = false;
                continue;
            }

            if (c == '\\')
            {
                escape = true;
                continue;
            }

            builder.Append(c);
        }

        return builder.ToString();
    }

    private static bool TryParseSource(ref ReadOnlySpan<char> s, out IrcMessageSource? result)
    {
        result = null;
        if (s[0] is not ':')
        {
            return true;
        }

        var spaceIndex = s.IndexOf(' ');
        if (spaceIndex < 0)
        {
            // TODO error for no end to source
            return false;
        }

        var sourceSpan = s[1..spaceIndex];
        s = s[spaceIndex..].SkipAll(' ');
        return IrcMessageSource.TryParse(sourceSpan, out result);
    }

    private static bool TryParseCommand(ref ReadOnlySpan<char> s, [NotNullWhen(true)] out string? result)
    {
        if (s.IsEmpty)
        {
            // TODO error about no command
            result = null;
            return false;
        }

        (var resultSpan, s) = s.SplitOnceConsecutive(' ');
        result = resultSpan.ToString();
        return true;
    }

    private static bool TryParseParameters(ref ReadOnlySpan<char> s, out ImmutableArray<string> result)
    {
        if (s.IsEmpty)
        {
            result = ImmutableArray<string>.Empty;
            return true;
        }

        var parameters = ImmutableArray.CreateBuilder<string>();
        while (!s.IsEmpty)
        {
            if (s[0] is ':')
            {
                parameters.Add(s[1..].ToString());
                s = ReadOnlySpan<char>.Empty;
                break;
            }

            (var p, s) = s.SplitOnceConsecutive(' ');
            parameters.Add(p.ToString());
        }

        result = parameters.DrainToImmutable();
        return true;
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

public sealed class IrcMessageSource : ISpanParsable<IrcMessageSource>
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
    public static IrcMessageSource Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        var (serverOrNick, user, host) = (s.IndexOf('!'), s.IndexOf('@')) switch
        {
            (< 0, < 0) => (s.ToString(), null, null),
            (var userIndex, < 0) => (s[..userIndex].ToString(), s[(userIndex + 1)..].ToString(), null),
            (< 0, var hostIndex) => (s[..hostIndex].ToString(), null, s[(hostIndex + 1)..].ToString()),
            (var userIndex, var hostIndex) => (
                s[..userIndex].ToString(),
                s[(userIndex + 1)..hostIndex].ToString(),
                s[(hostIndex + 1)..].ToString()),
        };

        return new IrcMessageSource
        {
            ServerOrNick = serverOrNick,
            User = user,
            Host = host,
        };
    }

    public static bool TryParse(ReadOnlySpan<char> s, [MaybeNullWhen(false)] out IrcMessageSource result) =>
        TryParse(s, null, out result);
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out IrcMessageSource result)
    {
        result = Parse(s, provider);
        return true;
    }

    public override string ToString()
    {
        var result = ServerOrNick;
        if (User is not null) result += $"!{User}";
        if (Host is not null) result += $"@{Host}";
        return result;
    }
}
