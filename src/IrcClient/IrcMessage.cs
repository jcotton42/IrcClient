﻿using System.Text;

namespace IrcClient;

public sealed class IrcMessage
{
    public required string Command { get; init; }
    public required string? Source { get; init;  }
    public required IReadOnlyDictionary<string, string?> Tags { get; init; }
    public required IReadOnlyList<string> Parameters { get; init; }

    public static IrcMessage Parse(ReadOnlySpan<char> message)
    {
        IReadOnlyDictionary<string, string?>? tags = null;
        IReadOnlyList<string>? parameters = null;
        string? source = null;
        string command;
        int nextSpace;
        if (message.StartsWith("@"))
        {
            nextSpace = message.IndexOf(" ");
            tags = ParseTags(message[1..nextSpace]).AsReadOnly();
            message = message[(nextSpace + 1)..];
        }

        if (message.StartsWith(":"))
        {
            nextSpace = message.IndexOf(" ");
            source = new string(message[1..nextSpace]);
            message = message[(nextSpace + 1)..];
        }

        nextSpace = message.IndexOf(" ");
        if (nextSpace != -1)
        {
            command = new string(message[..nextSpace]);
            parameters = ParseParameters(message[(nextSpace + 1)..]).AsReadOnly();
        }
        else
        {
            command = new string(message);
        }

        return new IrcMessage
        {
            Command = command,
            Source = source,
            Parameters = parameters ?? Array.Empty<string>().AsReadOnly(),
            Tags = tags ?? new Dictionary<string, string?>().AsReadOnly(),
        };
    }

    private static Dictionary<string, string?> ParseTags(ReadOnlySpan<char> span)
    {
        string key;
        string? value;
        Dictionary<string, string?> tags = new();
        for (; span.IndexOf(';') is var nextSplit and not -1; span = span[(nextSplit + 1)..])
        {
            (key, value) = ParseTag(span[..nextSplit]);
            tags[key] = value;
        }

        (key, value) = ParseTag(span);
        tags[key] = value;
        return tags;
    }

    private static (string, string?) ParseTag(ReadOnlySpan<char> span)
    {
        var equals = span.IndexOf('=');
        if (equals < 0) return (new string(span), null);
        var valueSpan = span[(equals + 1)..];
        if (valueSpan.IsEmpty) return (new string(span[..equals]), null);

        var escaped = false;
        StringBuilder value = new();
        foreach (var ch in valueSpan)
        {
            if (escaped)
            {
                value.Append(ch switch
                {
                    ':' => ';',
                    's' => ' ',
                    '\\' => '\\',
                    'r' => '\r',
                    'n' => '\n',
                    _ => ch,
                });
                escaped = false;
            }
            else if (ch == '\\')
            {
                escaped = true;
            }
            else
            {
                value.Append(ch);
            }
        }

        return (new string(span[..equals]), value.ToString());
    }

    private static List<string> ParseParameters(ReadOnlySpan<char> span)
    {
        if (span.StartsWith(":"))
        {
            return new List<string> { new string(span[1..]) };
        }

        string? trailing;
        ReadOnlySpan<char> middleSpan;
        List<string> parameters = new();
        var trailingSplit = span.IndexOf(" :");
        if (trailingSplit > 0)
        {
            trailing = new string(span[(trailingSplit + 2)..]);
            middleSpan = span[..trailingSplit];
        }
        else
        {
            trailing = null;
            middleSpan = span;
        }

        for (; middleSpan.IndexOf(' ') is var nextSplit and not -1; middleSpan = middleSpan[(nextSplit + 1)..])
        {
            parameters.Add(new string(middleSpan[..nextSplit]));
        }

        if (trailing is not null) parameters.Add(trailing);

        return parameters;
    }
}