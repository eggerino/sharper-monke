using System.Collections.Generic;
using Monkey.Ast;
using Monkey.Code;
using Monkey.Object;

namespace Monkey;

public record ByteCode(IReadOnlyList<byte> Instructions, IReadOnlyList<IObject> Constants);

public class Compiler
{
    private readonly List<byte> _instructions = [];
    private readonly List<IObject> _constants = [];

    public ByteCode GetByteCode() => new(_instructions, _constants);

    public string? Compile(INode node) => node switch
    {
        Program program => CompileProgram(program),
        ExpressionStatement exprStmt => CompileExpressionStatement(exprStmt),
        InfixExpression expr => CompileInfixExpression(expr),
        IntegerLiteral literal => CompileIntegerLiteral(literal),
        Ast.Boolean boolean => CompileBoolean(boolean),
        _ => null,
    };

    private string? CompileProgram(Program program)
    {
        foreach (var statement in program.Statements)
        {
            var error = Compile(statement);
            if (error is not null)
            {
                return error;
            }
        }
        return null;
    }

    private string? CompileExpressionStatement(ExpressionStatement expressionStatement)
    {
        if (expressionStatement.Expression is null)
        {
            return "No expression in the expressions statement";
        }
        var error = Compile(expressionStatement.Expression);
        if (error is not null)
        {
            return error;
        }

        Emit(Opcode.Pop);
        return null;
    }

    private string? CompileInfixExpression(InfixExpression infixExpression)
    {
        var (left, right) = infixExpression.Operator switch
        {
            "<" => (infixExpression.Right, infixExpression.Left),   // For less than -> switch order of branches and use greater than opcode 
            _ => (infixExpression.Left, infixExpression.Right),
        };

        var error = Compile(left);
        if (error is not null)
        {
            return error;
        }

        error = Compile(right);
        if (error is not null)
        {
            return error;
        }

        switch (infixExpression.Operator)
        {
            case "+":
                Emit(Opcode.Add);
                return null;

            case "-":
                Emit(Opcode.Sub);
                return null;

            case "*":
                Emit(Opcode.Mul);
                return null;

            case "/":
                Emit(Opcode.Div);
                return null;

            case "==":
                Emit(Opcode.Equal);
                return null;

            case "!=":
                Emit(Opcode.NotEqual);
                return null;

            case ">":
            case "<":                       // Uses same op code but different but changed order of operands to be semantically correct
                Emit(Opcode.GreaterThan);
                return null;

            default:
                return $"ERROR: unknown operator {infixExpression.Operator}";
        }
    }

    private string? CompileIntegerLiteral(IntegerLiteral integerLiteral)
    {
        var integer = new Integer(integerLiteral.Value);
        Emit(Opcode.Constant, AddConstant(integer));
        return null;
    }

    private string? CompileBoolean(Ast.Boolean boolean)
    {
        var op = boolean.Value switch
        {
            true => Opcode.True,
            false => Opcode.False,
        };
        Emit(op);
        return null;
    }

    private int AddConstant(IObject value)
    {
        _constants.Add(value);
        return _constants.Count - 1;
    }

    private int Emit(Opcode op, params int[] operands)
    {
        var ins = Instruction.Make(op, operands);
        return AddInstruction(ins);
    }

    private int AddInstruction(IEnumerable<byte> ins)
    {
        var positionNewInstructions = _instructions.Count;
        _instructions.AddRange(ins);
        return positionNewInstructions;
    }
}
