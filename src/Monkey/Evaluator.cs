using System.Collections.Generic;
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
            Program x => EvalStatements(x.Statements),
            ExpressionStatement x when x.Expression is not null => Eval(x.Expression),
            IntegerLiteral x => new Integer(x.Value),
            BlockStatement x => EvalStatements(x.Statements),
            Ast.Boolean x => NativeBoolToBooleanObject(x.Value),
            PrefixExpression x => EvalPrefixExpression(x.Operator, Eval(x.Right)),
            InfixExpression x => EvalInfixExpression(x.Operator, Eval(x.Left), Eval(x.Right)),
            IfExpression x => EvalIfExpression(x),
            _ => _null,
        };
    }

    private static IObject EvalStatements(IEnumerable<IStatement> statements)
    {
        IObject result = _null;
        foreach (var statement in statements)
        {
            result = Eval(statement);
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
            _ => _null,
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
            _ => _null,
        };
    }

    private static IObject EvalInfixExpression(string @operator, IObject left, IObject right)
    {
        return (@operator, left, right) switch
        {
            (_, Integer l, Integer r) => EvalIntegerInfixExpression(@operator, l, r),
            ("==", _, _) => NativeBoolToBooleanObject(left == right),
            ("!=", _, _) => NativeBoolToBooleanObject(left != right),
            _ => _null,
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
            _ => _null,
        };
    }

    private static IObject EvalIfExpression(IfExpression ifExpression)
    {
        return (IsTruthy(Eval(ifExpression.Condition)), ifExpression.Alternative) switch
        {
            (true, _) => Eval(ifExpression.Consequence),
            (false, BlockStatement a) => Eval(a),
            _ => _null,
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
