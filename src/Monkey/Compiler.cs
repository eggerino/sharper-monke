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
        return Compile(expressionStatement.Expression);
    }

    private string? CompileInfixExpression(InfixExpression infixExpression)
    {
        var error = Compile(infixExpression.Left);
        if (error is not null)
        {
            return error;
        }

        error = Compile(infixExpression.Right);
        if (error is not null)
        {
            return error;
        }

        switch (infixExpression.Operator)
        {
            case "+":
                Emit(Opcode.Add);
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
