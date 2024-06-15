using Monkey.Ast;
using Monkey.Object;

namespace Monkey;

public static class Evaluator
{
    private static readonly Null _null = new();
    private static readonly Object.Boolean _true = new(true);
    private static readonly Object.Boolean _false = new(false);

    public static IObject Eval(INode node)
    {
        return node switch
        {
            Program x => EvalProgram(x),
            ExpressionStatement x when x.Expression is not null => Eval(x.Expression),
            IntegerLiteral x => new Integer(x.Value),
            BlockStatement x => EvalBlockStatements(x),
            ReturnStatement x => Eval(x.ReturnValue) switch
            {
                Error e => e,
                var v => new ReturnValue(v),
            },
            Ast.Boolean x => NativeBoolToBooleanObject(x.Value),
            PrefixExpression x => Eval(x.Right) switch
            {
                Error e => e,
                var r => EvalPrefixExpression(x.Operator, r),
            },
            InfixExpression x => Eval(x.Left) switch
            {
                Error e => e,
                var l => Eval(x.Right) switch
                {
                    Error e => e,
                    var r => EvalInfixExpression(x.Operator, l, r),
                },
            },
            IfExpression x => EvalIfExpression(x),
            _ => _null,
        };
    }

    private static IObject EvalProgram(Program program)
    {
        IObject result = _null;
        foreach (var statement in program.Statements)
        {
            result = Eval(statement);
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

    private static IObject EvalBlockStatements(BlockStatement blockStatement)
    {
        IObject result = _null;
        foreach (var statement in blockStatement.Statements)
        {
            result = Eval(statement);

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

    private static IObject EvalIfExpression(IfExpression ifExpression)
    {
        return Eval(ifExpression.Condition) switch
        {
            Error e => e,
            var c => (IsTruthy(c), ifExpression.Alternative) switch
            {
                (true, _) => Eval(ifExpression.Consequence),
                (false, BlockStatement a) => Eval(a),
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
}
