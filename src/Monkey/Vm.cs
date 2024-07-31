using System;
using System.Collections.Generic;
using System.Linq;
using Monkey.Code;
using Monkey.Object;

namespace Monkey;

public class Vm
{
    private const int StackSize = 2048;

    private static readonly Object.Boolean _true = new Object.Boolean(true);
    private static readonly Object.Boolean _false = new Object.Boolean(false);
    private static readonly Null _null = new Null();

    private readonly ArraySegment<byte> _instructions;
    private readonly IReadOnlyList<IObject> _constants;

    private readonly IObject[] _stack;
    private int _stackPointer;

    public Vm(ByteCode byteCode)
    {
        _instructions = byteCode.Instructions.ToArray().AsSegment();
        _constants = byteCode.Constants;

        _stack = new IObject[StackSize];
        _stackPointer = 0;
    }

    public IObject? GetStackTop() => _stackPointer switch
    {
        var x when x < 1 => null,
        _ => _stack[_stackPointer - 1],
    };

    public IObject GetLastPoppedStackElement() => _stack[_stackPointer];

    public string? Run()
    {
        for (var ip = 0; ip < _instructions.Count; ip++)
        {
            var op = _instructions[ip].AsOpcode();

            string? error;
            int pos;
            switch (op)
            {
                case Opcode.Constant:
                    var constIndex = Instruction.ReadUint16(_instructions.Slice(ip + 1));
                    ip += 2;

                    error = Push(_constants[constIndex]);
                    if (error is not null)
                    {
                        return error;
                    }
                    break;

                case Opcode.Pop:
                    Pop();
                    break;

                case Opcode.Add:
                case Opcode.Sub:
                case Opcode.Mul:
                case Opcode.Div:
                    error = ExecuteBinaryOperation(op);
                    if (error is not null)
                    {
                        return error;
                    }
                    break;

                case Opcode.True:
                    error = Push(_true);
                    if (error is not null)
                    {
                        return error;
                    }
                    break;

                case Opcode.False:
                    error = Push(_false);
                    if (error is not null)
                    {
                        return error;
                    }
                    break;

                case Opcode.Equal:
                case Opcode.NotEqual:
                case Opcode.GreaterThan:
                    error = ExecuteComparison(op);
                    if (error is not null)
                    {
                        return error;
                    }
                    break;

                case Opcode.Minus:
                    error = ExecuteMinusOperator();
                    if (error is not null)
                    {
                        return error;
                    }
                    break;

                case Opcode.Bang:
                    error = ExecuteBangOperator();
                    if (error is not null)
                    {
                        return error;
                    }
                    break;

                case Opcode.JumpNotTruthy:
                    pos = Instruction.ReadUint16(_instructions.Slice(ip + 1));
                    ip += 2;    // Skip the operands no matter the condition
                    var condition = Pop();

                    if (!IsTruthy(condition))
                    {
                        ip = pos - 1;
                    }
                    break;

                case Opcode.Jump:
                    pos = Instruction.ReadUint16(_instructions.Slice(ip + 1));
                    ip = pos - 1;   // end of iteration will increment ip again
                    break;

                case Opcode.Null:
                    error = Push(_null);
                    if (error is not null)
                    {
                        return error;
                    }
                    break;
            }
        }

        return null;
    }

    private string? ExecuteBinaryOperation(Opcode op)
    {
        var right = Pop();
        var left = Pop();

        if (left is Integer leftValue && right is Integer rightValue)
        {
            return ExecuteBinaryIntegerOperation(op, leftValue, rightValue);
        }

        return $"ERROR: Unsupported types for binary operation: {left.GetType()} {right.GetType()}";
    }

    private string? ExecuteBinaryIntegerOperation(Opcode op, Integer left, Integer right)
    {
        long result = 0;
        switch (op)
        {
            case Opcode.Add:
                result = left.Value + right.Value;
                break;

            case Opcode.Sub:
                result = left.Value - right.Value;
                break;

            case Opcode.Mul:
                result = left.Value * right.Value;
                break;

            case Opcode.Div:
                result = left.Value / right.Value;
                break;

            default:
                return $"ERROR: unknown integer operator: {op}";
        }

        return Push(new Integer(result));
    }

    private string? ExecuteComparison(Opcode op)
    {
        var right = Pop();
        var left = Pop();


        if (left is Integer leftValue && right is Integer rightValue)
        {
            return ExecuteIntegerComparison(op, leftValue, rightValue);
        }

        return op switch
        {
            Opcode.Equal => Push(NativeBoolToBooleanObject(left == right)),
            Opcode.NotEqual => Push(NativeBoolToBooleanObject(left != right)),
            _ => $"ERROR: unknown operator: {op} ({left.GetType()} {right.GetType()})",
        };
    }

    private string? ExecuteIntegerComparison(Opcode op, Integer left, Integer right) => op switch
    {
        Opcode.Equal => Push(NativeBoolToBooleanObject(left.Value == right.Value)),
        Opcode.NotEqual => Push(NativeBoolToBooleanObject(left.Value != right.Value)),
        Opcode.GreaterThan => Push(NativeBoolToBooleanObject(left.Value > right.Value)),
        _ => $"ERROR: unknown operator: {op}",
    };

    private static Object.Boolean NativeBoolToBooleanObject(bool value) => value switch
    {
        true => _true,
        false => _false,
    };

    private string? ExecuteMinusOperator()
    {
        var operand = Pop();

        if (operand is not Integer integer)
        {
            return $"ERROR: unsupported type for negation: {operand.GetType()}";
        }

        return Push(new Integer(-integer.Value));
    }

    private string? ExecuteBangOperator()
    {
        var operand = Pop();

        return operand switch
        {
            Object.Boolean x when x == _true => Push(_false),
            Object.Boolean x when x == _false => Push(_true),
            Null => Push(_true),
            _ => Push(_false),
        };
    }

    private static bool IsTruthy(IObject value) => value switch
    {
        Object.Boolean x => x.Value,
        Null => false,
        _ => true,
    };

    private string? Push(IObject value)
    {
        if (_stackPointer >= StackSize)
        {
            return "ERROR: stack overflow";
        }

        _stack[_stackPointer] = value;
        _stackPointer++;

        return null;
    }

    private IObject Pop()
    {
        var value = _stack[_stackPointer - 1];
        _stackPointer--;
        return value;
    }
}
