using System.Collections.Generic;
using System.Linq;
using Monkey.Ast;
using Monkey.Object;

namespace Monkey;

public static class Evaluator
{
    private static readonly Null _null = new();
    private static readonly Object.Boolean _true = new(true);
    private static readonly Object.Boolean _false = new(false);

    public static IObject Eval(INode node, Environment environment)
    {
        return node switch
        {
            Program x => EvalProgram(x, environment),
            ExpressionStatement x when x.Expression is not null => Eval(x.Expression, environment),
            IntegerLiteral x => new Integer(x.Value),
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
            CallExpression x => Eval(x.Function, environment) switch
            {
                Error e => e,
                var f => EvalExpressions(x.Arguments, environment) switch
                {
                [Error e] => e,
                    var a => ApplyFunction(f, a),
                },
            },
            _ => _null,
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
        IObject result = _null;
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
        IObject result = _null;
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

    private static Object.Boolean NativeBoolToBooleanObject(bool value) => value ? _true : _false;

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
            Object.Boolean x when x.Value => _false,
            Object.Boolean x when !x.Value => _true,
            Null _ => _true,
            _ => _false,
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

    private static IObject EvalIfExpression(IfExpression ifExpression, Environment environment)
    {
        return Eval(ifExpression.Condition, environment) switch
        {
            Error e => e,
            var c => (IsTruthy(c), ifExpression.Alternative) switch
            {
                (true, _) => Eval(ifExpression.Consequence, environment),
                (false, BlockStatement a) => Eval(a, environment),
                _ => _null,
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
            null => new Error($"identifier not found: {identifier.Value}"),
        };
    }

    private static IObject ApplyFunction(IObject function, IEnumerable<IObject> arguments)
    {
        return function switch
        {
            Function f => UnwrapReturnValue(Eval(f.Body, ExtendFunctionEnvironment(f, arguments))),
            _ => new Error($"not a function: {function.GetObjectType()}"),
        };
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
}
