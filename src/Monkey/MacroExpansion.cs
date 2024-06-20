using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Monkey.Ast;
using Monkey.Object;

namespace Monkey;

public static class MacroExpansion
{
    public static Program DefineMacros(Program program, Environment environment)
    {
        foreach (var (identifier, macro) in program.Statements
            .Select(TryGetMacroDefinition)
            .Where(x => x.HasValue)
            .Select(x => x!.Value))
        {
            AddMacro(identifier, macro, environment);
        }

        return program with { Statements = program.Statements.Where(s => !TryGetMacroDefinition(s).HasValue).ToImmutableList() };
    }

    private static (Identifier Identifier, MacroLiteral Macro)? TryGetMacroDefinition(IStatement statement)
    {
        return statement switch
        {
            LetStatement let when let.Value is MacroLiteral macro => (let.Name, macro),
            _ => null,
        };
    }

    private static void AddMacro(Identifier identifier, MacroLiteral macro, Environment environment)
    {
        environment.Set(identifier.Value, new Macro(macro.Parameters, macro.Body, environment));
    }

    public static INode ExpandMacros(INode program, Environment environment)
    {
        return program.Transform(node =>
        {
            return node switch
            {
                CallExpression call when TryGetMacroCall(call, environment) is Macro macro => ExpandMacro(call, macro),
                _ => node,
            };
        });
    }

    private static Macro? TryGetMacroCall(CallExpression call, Environment environment)
    {
        return call.Function switch
        {
            Identifier identifier when environment.Get(identifier.Value) is Macro macro => macro,
            _ => null,
        };
    }

    private static INode ExpandMacro(CallExpression call, Macro macro)
    {
        var args = call.Arguments.Select(a => new Quote(a));
        var evalEnv = ExtendMacroEnvironment(macro, args);

        var evaluated = Evaluator.Eval(macro.Body, evalEnv);

        if (evaluated is Quote quote)
        {
            if (quote.Node is null)
            {
                return new BlockStatement(new(TokenType.Illegal, ""), []);
            }
            return quote.Node;
        }

        throw new System.Exception("we only support returning AST-nodes from macros");
    }

    private static Environment ExtendMacroEnvironment(Macro macro, IEnumerable<Quote> args)
    {
        var extended = macro.Environment.NewEnclosedEnvironment();

        foreach (var (parameter, arg) in macro.Parameters.Zip(args))
        {
            extended.Set(parameter.Value, arg);
        }

        return extended;
    }
}
