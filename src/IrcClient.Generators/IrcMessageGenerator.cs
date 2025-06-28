using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IrcClient.Generators;

[Generator]
public sealed class IrcMessageGenerator : IIncrementalGenerator
{
    private const string AttributeName = "IrcClient.Messages.IrcMessageAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var messages = context.SyntaxProvider.ForAttributeWithMetadataName(
            AttributeName,
            (node, _) => node is RecordDeclarationSyntax,
            GetMessages)
            .Where(message => message is not null);
    }

    private static IrcMessageData? GetMessages(GeneratorAttributeSyntaxContext context, CancellationToken conCancellationToken)
    {
        if (context.Attributes is not [{ ConstructorArguments: [{ Value: string command }] }])
        {
            return null;
        }

        var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;
        var constructorArities = new EquatableList<int>();
        foreach (var constructor in typeSymbol.Constructors)
        {
            if (constructor.IsStatic)
            {
                continue;
            }

            var isPrimary = IsPrimaryConstructor(constructor);

            var arity = 0;
            foreach (var parameterSymbol in constructor.Parameters)
            {
                if (parameterSymbol.IsOptional)
                {
                    constructorArities.Add(arity);
                }
                arity++;
            }
            constructorArities.Add(arity);
        }
    }

    private static bool IsPrimaryConstructor(IMethodSymbol methodSymbol) =>
        methodSymbol.DeclaringSyntaxReferences is [var syntaxRef, ..]
        && syntaxRef.GetSyntax() is RecordDeclarationSyntax;
}

public sealed record IrcMessageData(string Command, EquatableList<int> ConstructorArities);
