using System;

namespace IrcClient.Messages;

[AttributeUsage(AttributeTargets.Class)]
public sealed class IrcMessageAttribute(string command) : Attribute
{
    public string Command { get; } = command;
}

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class IrcParameterAttribute : Attribute
{
    public char ListDelimiter { get; init; }
}
