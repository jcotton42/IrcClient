﻿using System.Text;

using IrcClient.Infrastructure;

namespace IrcClient;

public sealed class IrcMessage : IMessage
{
    public static IrcMessageFactory Factory { get; } = new();
    public required string Command { get; init; }
    public required string? Source { get; init;  }
    public required IReadOnlyDictionary<string, string?> Tags { get; init; }
    public required IReadOnlyList<string> Parameters { get; init; }

    public static IrcMessage Create(string command, params string[] parameters)
    {
        return new IrcMessage
        {
            Tags = new Dictionary<string, string?>().AsReadOnly(),
            Source = null,
            Command = command,
            Parameters = parameters.AsReadOnly(),
        };
    }

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
        StringBuilder valueBuilder = new();
        for (; span.IndexOf(';') is var nextSplit and not -1; span = span[(nextSplit + 1)..])
        {
            (key, value) = ParseTag(span[..nextSplit], valueBuilder);
            tags[key] = value;
            valueBuilder.Clear();
        }

        (key, value) = ParseTag(span, valueBuilder);
        tags[key] = value;
        return tags;
    }

    private static (string, string?) ParseTag(ReadOnlySpan<char> span, StringBuilder value)
    {
        var equals = span.IndexOf('=');
        if (equals < 0) return (new string(span), null);
        var valueSpan = span[(equals + 1)..];
        if (valueSpan.IsEmpty) return (new string(span[..equals]), null);

        var escaped = false;
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
            var toParse = middleSpan[..nextSplit];
            if (toParse.IsEmpty) continue;
            parameters.Add(new string(toParse));
        }
        if (!middleSpan.IsEmpty) parameters.Add(new string(middleSpan));

        if (trailing is not null) parameters.Add(trailing);

        return parameters;
    }

    public override string ToString()
    {
        StringBuilder message = new();
        if (Tags.Any())
        {
            message.Append('@');
            var count = 0;
            foreach(var (key, value) in Tags)
            {
                message.Append(key);
                if (value is not null)
                {
                    message.Append('=');
                    AppendEscapedTagValue(message, value);
                }

                count++;
                if (count < Tags.Count) message.Append(';');
            }

            message.Append(' ');
        }

        if (Source is not null)
        {
            message.Append(':');
            message.Append(Source);
            message.Append(' ');
        }

        message.Append(Command);

        if (Parameters.Any())
        {
            foreach (var param in Parameters.SkipLast(1))
            {
                message.Append(' ');
                message.Append(param);
            }

            message.Append(" :");
            message.Append(Parameters[^1]);
        }

        return message.ToString();
    }

    private void AppendEscapedTagValue(StringBuilder message, string value)
    {
        foreach (var ch in value)
        {
            switch (ch)
            {
                case ';':
                    message.Append("\\:");
                    break;
                case ' ':
                    message.Append("\\s");
                    break;
                case '\\':
                    message.Append("\\\\");
                    break;
                case '\r':
                    message.Append("\\r");
                    break;
                case '\n':
                    message.Append("\\n");
                    break;
                default:
                    message.Append(ch);
                    break;
            }
        }
    }
}

/// <summary>
/// Empty type, to be used as a place for extension methods on <see cref="IrcMessage.Factory"/>.
/// </summary>
public sealed class IrcMessageFactory { }
