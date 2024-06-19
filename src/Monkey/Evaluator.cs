using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Monkey.Ast;
using Monkey.Object;

namespace Monkey;

public static class Evaluator
{
    public static Null Null { get; } = new();
    public static Object.Boolean True { get; } = new(true);
    public static Object.Boolean False { get; } = new(false);

    public static IObject Eval(INode node, Environment environment)
    {
        return node switch
        {
            Program x => EvalProgram(x, environment),
            ExpressionStatement x when x.Expression is not null => Eval(x.Expression, environment),
            IntegerLiteral x => new Integer(x.Value),
            StringLiteral x => new String(x.Value),
            BlockStatement x => EvalBlockStatements(x, environment),
            ReturnStatement x => Eval(x.ReturnValue, environment) switch
            {
                Error e => e,
                var v => new ReturnValue(v),
            },
            LetStatement x => Eval(x.Value, environment) switch
            {
                Error e => e,
                var v => environment.Set(x.Name.Value, v)
            },
            Ast.Boolean x => NativeBoolToBooleanObject(x.Value),
            PrefixExpression x => Eval(x.Right, environment) switch
            {
                Error e => e,
                var r => EvalPrefixExpression(x.Operator, r),
            },
            InfixExpression x => Eval(x.Left, environment) switch
            {
                Error e => e,
                var l => Eval(x.Right, environment) switch
                {
                    Error e => e,
                    var r => EvalInfixExpression(x.Operator, l, r),
                },
            },
            IfExpression x => EvalIfExpression(x, environment),
            Identifier x => EvalIdentifier(x, environment),
            FunctionLiteral x => new Function(x.Parameters, x.Body, environment),
            CallExpression x when x.Function.GetTokenLiteral() == "quote" => Quote(x.Arguments.FirstOrDefault(), environment),
            CallExpression x => Eval(x.Function, environment) switch
            {
                Error e => e,
                var f => EvalExpressions(x.Arguments, environment) switch
                {
                [Error e] => e,
                    var a => ApplyFunction(f, a),
                },
            },
            ArrayLiteral x => EvalExpressions(x.Elements, environment) switch
            {
            [Error e] => e,
                var elements => new Array(elements.ToImmutableList()),
            },
            IndexExpression x => Eval(x.Left, environment) switch
            {
                Error e => e,
                var l => Eval(x.Index, environment) switch
                {
                    Error e => e,
                    var i => EvalIndexExpression(l, i),
                },
            },
            HashLiteral x => EvalHashLiteral(x, environment),
            _ => Null,
        };
    }

    private static List<IObject> EvalExpressions(IEnumerable<IExpression> expressions, Environment environment)
    {
        var objects = new List<IObject>();
        foreach (var expression in expressions)
        {
            var @object = Eval(expression, environment);
            if (@object is Error)
            {
                return [@object];
            }

            objects.Add(@object);
        }
        return objects;
    }

    private static IObject EvalProgram(Program program, Environment environment)
    {
        IObject result = Null;
        foreach (var statement in program.Statements)
        {
            result = Eval(statement, environment);
            if (result is ReturnValue returnValue)
            {
                return returnValue.Value;
            }
            else if (result is Error)
            {
                return result;
            }
        }
        return result;
    }

    private static IObject EvalBlockStatements(BlockStatement blockStatement, Environment environment)
    {
        IObject result = Null;
        foreach (var statement in blockStatement.Statements)
        {
            result = Eval(statement, environment);

            if (result is ReturnValue)
            {
                return result;
            }
            else if (result is Error)
            {
                return result;
            }
        }
        return result;
    }

    private static Object.Boolean NativeBoolToBooleanObject(bool value) => value ? True : False;

    private static IObject EvalPrefixExpression(string @operator, IObject right)
    {
        return @operator switch
        {
            "!" => EvaluateBangOperatorExpression(right),
            "-" => EvaluateMinusPrefixOperatorExpression(right),
            _ => new Error($"unknown operator: {@operator} {right.GetObjectType()}"),
        };
    }

    private static Object.Boolean EvaluateBangOperatorExpression(IObject right)
    {
        return right switch
        {
            Object.Boolean x when x.Value => False,
            Object.Boolean x when !x.Value => True,
            Null _ => True,
            _ => False,
        };
    }

    private static IObject EvaluateMinusPrefixOperatorExpression(IObject right)
    {
        return right switch
        {
            Integer x => new Integer(-x.Value),
            _ => new Error($"unknown operator: -{right.GetObjectType()}"),
        };
    }

    private static IObject EvalInfixExpression(string @operator, IObject left, IObject right)
    {
        return (@operator, left, right) switch
        {
            (_, Integer l, Integer r) => EvalIntegerInfixExpression(@operator, l, r),
            (_, String l, String r) => EvalStringInfixExpression(@operator, l, r),
            ("==", _, _) => NativeBoolToBooleanObject(left == right),
            ("!=", _, _) => NativeBoolToBooleanObject(left != right),
            _ when left.GetObjectType() != right.GetObjectType() => new Error($"type mismatch: {left.GetObjectType()} {@operator} {right.GetObjectType()}"),
            _ => new Error($"unknown operator: {left.GetObjectType()} {@operator} {right.GetObjectType()}"),
        };
    }

    private static IObject EvalIntegerInfixExpression(string @operator, Integer left, Integer right)
    {
        return @operator switch
        {
            "+" => new Integer(left.Value + right.Value),
            "-" => new Integer(left.Value - right.Value),
            "*" => new Integer(left.Value * right.Value),
            "/" => new Integer(left.Value / right.Value),
            "<" => NativeBoolToBooleanObject(left.Value < right.Value),
            ">" => NativeBoolToBooleanObject(left.Value > right.Value),
            "==" => NativeBoolToBooleanObject(left.Value == right.Value),
            "!=" => NativeBoolToBooleanObject(left.Value != right.Value),
            _ => new Error($"unknown operator: {left.GetObjectType()} {@operator} {right.GetObjectType()}"),
        };
    }

    private static IObject EvalStringInfixExpression(string @operator, String left, String right)
    {
        return @operator switch
        {
            "+" => new String(left.Value + right.Value),
            "==" => NativeBoolToBooleanObject(left.Value == right.Value),
            "!=" => NativeBoolToBooleanObject(left.Value != right.Value),
            _ => new Error($"unknown operator: {left.GetObjectType()} {@operator} {right.GetObjectType()}"),
        };
    }

    private static IObject EvalIfExpression(IfExpression ifExpression, Environment environment)
    {
        return Eval(ifExpression.Condition, environment) switch
        {
            Error e => e,
            var c => (IsTruthy(c), ifExpression.Alternative) switch
            {
                (true, _) => Eval(ifExpression.Consequence, environment),
                (false, BlockStatement a) => Eval(a, environment),
                _ => Null,
            },
        };
    }

    private static bool IsTruthy(IObject obj)
    {
        return obj switch
        {
            Null _ => false,
            Object.Boolean x => x.Value,
            _ => true,
        };
    }

    private static IObject EvalIdentifier(Identifier identifier, Environment environment)
    {
        return environment.Get(identifier.Value) switch
        {
            IObject x => x,
            null => Builtins.Get(identifier.Value) switch
            {
                Builtin x => x,
                null => new Error($"identifier not found: {identifier.Value}"),
            }
        };
    }

    private static IObject ApplyFunction(IObject function, IEnumerable<IObject> arguments)
    {
        return function switch
        {
            Function f => UnwrapReturnValue(Eval(f.Body, ExtendFunctionEnvironment(f, arguments))),
            Builtin f => f.Function(arguments),
            _ => new Error($"not a function: {function.GetObjectType()}"),
        };
    }

    private static IObject EvalIndexExpression(IObject left, IObject index)
    {
        return (left, index) switch
        {
            (Array a, Integer i) => EvalArrayIndexExpression(a, i),
            (Hash h, IObject key) => EvalHashIndexExpression(h, key),
            _ => new Error($"index operator not supported: {left.GetObjectType()}"),
        };
    }

    private static IObject EvalArrayIndexExpression(Array array, Integer index)
    {
        var elements = array.Elements;
        var i = index.Value;

        if (i < 0 || i >= elements.Count)
        {
            return Null;
        }

        return elements[(int)i];
    }

    private static IObject EvalHashIndexExpression(Hash hash, IObject key)
    {
        return key switch
        {
            IHashable k => hash.Pairs.TryGetValue(k, out var value) ? value : Null,
            _ => new Error($"unusable as hash key: {key.GetObjectType()}"),
        };
    }

    private static IObject EvalHashLiteral(HashLiteral hash, Environment environment)
    {
        var pairs = ImmutableDictionary<IHashable, IObject>.Empty.ToBuilder();

        foreach (var (keyExpression, valueExpression) in hash.Pairs)
        {
            var key = Eval(keyExpression, environment);
            if (key is Error)
            {
                return key;
            }

            if (key is not IHashable)
            {
                return new Error($"unusable as hash key: {key.GetObjectType()}");
            }

            var value = Eval(valueExpression, environment);
            if (value is Error)
            {
                return value;
            }

            pairs.Add((IHashable)key, value);
        }

        return new Hash(pairs.ToImmutable());
    }

    private static Environment ExtendFunctionEnvironment(Function function, IEnumerable<IObject> arguments)
    {
        var env = function.Environment.NewEnclosedEnvironment();

        foreach (var (parameter, argument) in function.Parameters.Zip(arguments))
        {
            env.Set(parameter.Value, argument);
        }
        return env;
    }

    private static IObject UnwrapReturnValue(IObject obj)
    {
        return obj switch
        {
            ReturnValue x => x.Value,
            _ => obj,
        };
    }

    private static Quote Quote(INode? node, Environment environment)
    {
        return new(node is null ? null : EvalUnquoteCalls(node, environment));
    }

    private static INode EvalUnquoteCalls(INode quoted, Environment environment)
    {
        return quoted.Transform(node => node switch
        {
            CallExpression x when x.Function.GetTokenLiteral() == "unquote" && x.Arguments.Count == 1 => ToNode(Eval(x.Arguments.First(), environment)),
            _ => node,
        });
    }

    private static INode ToNode(IObject obj) => obj switch
    {
        Integer x => new IntegerLiteral(new(TokenType.Integer, x.Value.ToString()), x.Value),
        Object.Boolean x when x.Value => new Ast.Boolean(new(TokenType.True, "true"), true),
        Object.Boolean x when !x.Value => new Ast.Boolean(new(TokenType.False, "false"), false),
        Quote x when x.Node is not null => x.Node,
        _ => new BlockStatement(new(TokenType.Illegal, ""), []),
    };
}
