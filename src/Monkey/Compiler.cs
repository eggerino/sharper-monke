using System.Collections.Generic;
using System.Linq;
using Monkey.Ast;
using Monkey.Code;
using Monkey.Object;

namespace Monkey;

public record ByteCode(IReadOnlyList<byte> Instructions, IReadOnlyList<IObject> Constants);

public class Compiler
{
    record EmittedInstruction(Opcode Opcode, int Position);

    private EmittedInstruction _lastInstruction = new(0, -1);
    private EmittedInstruction _previousInstruction = new(0, -1);

    private readonly SymbolTable _symbolTable;

    private readonly List<byte> _instructions = [];
    private readonly List<IObject> _constants;

    public ByteCode GetByteCode() => new(_instructions, _constants);

    private Compiler(SymbolTable table, List<IObject> constants)
    {
        _symbolTable = table;
        _constants = constants;
    }

    public Compiler() : this(new(), []) { }

    public static Compiler NewWithState(SymbolTable table, List<IObject> constants) => new(table, constants);

    public string? Compile(INode node) => node switch
    {
        Program program => CompileProgram(program),
        BlockStatement block => CompileBlockStatement(block),
        ExpressionStatement exprStmt => CompileExpressionStatement(exprStmt),
        LetStatement letStatement => CompileLetStatement(letStatement),
        Identifier ident => CompileIdentifier(ident),
        InfixExpression expr => CompileInfixExpression(expr),
        IntegerLiteral literal => CompileIntegerLiteral(literal),
        StringLiteral literal => CompileStringLiteral(literal),
        ArrayLiteral literal => CompileArrayLiteral(literal),
        HashLiteral literal => CompileHashLiteral(literal),
        Ast.Boolean boolean => CompileBoolean(boolean),
        PrefixExpression expr => CompilePrefixExpression(expr),
        IfExpression expr => CompileIfExpression(expr),
        IndexExpression expr => CompileIndexExpression(expr),
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

    private string? CompileBlockStatement(BlockStatement block)
    {
        foreach (var statement in block.Statements)
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

    private string? CompileLetStatement(LetStatement letStatement)
    {
        var error = Compile(letStatement.Value);
        if (error is not null)
        {
            return error;
        }

        var symbol = _symbolTable.Define(letStatement.Name.Value);
        Emit(Opcode.SetGlobal, symbol.Index);

        return null;
    }

    private string? CompileIdentifier(Identifier ident)
    {
        var symbol = _symbolTable.Resolve(ident.Value);
        if (symbol is null)
        {
            return $"ERROR: undefined variable {ident.Value}";
        }

        Emit(Opcode.GetGlobal, symbol.Index);
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

    private string? CompileStringLiteral(StringLiteral literal)
    {
        var str = new String(literal.Value);
        Emit(Opcode.Constant, AddConstant(str));
        return null;
    }

    private string? CompileArrayLiteral(ArrayLiteral literal)
    {
        foreach (var element in literal.Elements)
        {
            var error = Compile(element);
            if (error is not null)
            {
                return error;
            }
        }
        Emit(Opcode.Array, literal.Elements.Count);
        return null;
    }

    private string? CompileHashLiteral(HashLiteral literal)
    {
        foreach (var (key, value) in literal.Pairs)
        {
            var error = Compile(key);
            if (error is not null)
            {
                return error;
            }

            error = Compile(value);
            if (error is not null)
            {
                return error;
            }
        }
        Emit(Opcode.Hash, literal.Pairs.Count * 2);
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

    private string? CompilePrefixExpression(PrefixExpression expr)
    {
        var error = Compile(expr.Right);
        if (error is not null)
        {
            return error;
        }

        switch (expr.Operator)
        {
            case "!":
                Emit(Opcode.Bang);
                break;

            case "-":
                Emit(Opcode.Minus);
                break;

            default:
                return $"ERROR: unknown operator {expr.Operator}";
        }
        return null;
    }

    private string? CompileIfExpression(IfExpression expr)
    {
        var error = Compile(expr.Condition);
        if (error is not null)
        {
            return error;
        }

        // Bogus offset since it cannot be known before compiling the consequence
        var jumpNotTruthyPosition = Emit(Opcode.JumpNotTruthy, 9999);

        error = Compile(expr.Consequence);
        if (error is not null)
        {
            return error;
        }

        if (LastInstructionIsPop())
        {
            RemoveLastPop();
        }

        // Bogus offset since it cannot be known before compiling the alternative
        var jumpPosition = Emit(Opcode.Jump, 9999);

        // Patch the offset of the jump not truthy instruction
        var afterConsequencePosition = _instructions.Count;
        ChangeOperands(jumpNotTruthyPosition, afterConsequencePosition);

        if (expr.Alternative is null)
        {
            Emit(Opcode.Null);
        }
        else
        {
            error = Compile(expr.Alternative);
            if (error is not null)
            {
                return error;
            }

            if (LastInstructionIsPop())
            {
                RemoveLastPop();
            }
        }

        // Patch the offset of the jump instruction
        var afterAlternativePosition = _instructions.Count;
        ChangeOperands(jumpPosition, afterAlternativePosition);

        return null;
    }

    private bool LastInstructionIsPop() => _lastInstruction.Opcode == Opcode.Pop;

    private void RemoveLastPop()
    {
        var amountToRemove = _instructions.Count - _lastInstruction.Position;
        _instructions.RemoveRange(_lastInstruction.Position, amountToRemove);
    }

    private string? CompileIndexExpression(IndexExpression expr)
    {
        var error = Compile(expr.Left);
        if (error is not null)
        {
            return error;
        }

        error = Compile(expr.Index);
        if (error is not null)
        {
            return error;
        }

        Emit(Opcode.Index);
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
        var pos = AddInstruction(ins);

        SetLastInstruction(op, pos);

        return pos;
    }

    private int AddInstruction(IEnumerable<byte> ins)
    {
        var positionNewInstructions = _instructions.Count;
        _instructions.AddRange(ins);
        return positionNewInstructions;
    }

    private void SetLastInstruction(Opcode opcode, int position)
    {
        (_lastInstruction, _previousInstruction) = (new(opcode, position), _lastInstruction);
    }

    private void ChangeOperands(int opPos, params int[] operands)
    {
        var op = _instructions[opPos].AsOpcode();
        var newInstruction = Instruction.Make(op, operands).ToArray();

        ReplaceInstructions(opPos, newInstruction);
    }

    private void ReplaceInstructions(int position, byte[] newInstruction)
    {
        for (var i = 0; i < newInstruction.Length; i++)
        {
            _instructions[position + i] = newInstruction[i];
        }
    }
}
