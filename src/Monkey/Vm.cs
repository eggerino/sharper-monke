using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Monkey.Code;
using Monkey.Object;

namespace Monkey;

public class Vm
{
    private const int StackSize = 2048;
    private const int GlobalsSize = 2048;

    private static readonly Object.Boolean _true = new Object.Boolean(true);
    private static readonly Object.Boolean _false = new Object.Boolean(false);
    private static readonly Null _null = new Null();

    private readonly ArraySegment<byte> _instructions;
    private readonly IReadOnlyList<IObject> _constants;

    private readonly IObject[] _stack;
    private readonly IObject[] _globals;
    private int _stackPointer;

    private Vm(ByteCode byteCode, IObject[] globals)
    {
        _instructions = byteCode.Instructions.ToArray().AsSegment();
        _constants = byteCode.Constants;

        _stack = new IObject[StackSize];
        _globals = globals;
        _stackPointer = 0;
    }

    public Vm(ByteCode byteCode) : this(byteCode, CreateGlobalsArray()) { }

    public static Vm NewWithGlobalStore(ByteCode byteCode, IObject[] globals) => new(byteCode, globals);

    public static IObject[] CreateGlobalsArray() => new IObject[GlobalsSize];

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
            int globalIndex;
            int numElements;
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

                case Opcode.SetGlobal:
                    globalIndex = Instruction.ReadUint16(_instructions.Slice(ip + 1));
                    ip += 2;

                    _globals[globalIndex] = Pop();
                    break;

                case Opcode.GetGlobal:
                    globalIndex = Instruction.ReadUint16(_instructions.Slice(ip + 1));
                    ip += 2;

                    error = Push(_globals[globalIndex]);
                    if (error is not null)
                    {
                        return error;
                    }
                    break;

                case Opcode.Array:
                    numElements = Instruction.ReadUint16(_instructions.Slice(ip + 1));
                    ip += 2;

                    var array = BuildArray(_stackPointer - numElements, _stackPointer);
                    _stackPointer -= numElements;
                    error = Push(array);
                    if (error is not null)
                    {
                        return error;
                    }
                    break;

                case Opcode.Hash:
                    numElements = Instruction.ReadUint16(_instructions.Slice(ip + 1));
                    ip += 2;

                    (var hash, error) = BuildHash(_stackPointer - numElements, _stackPointer);
                    if (error is not null)
                    {
                        return error;
                    }

                    _stackPointer -= numElements;
                    error = Push(hash!);
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

        return (left, right) switch
        {
            (Integer leftInt, Integer rightInt) => ExecuteBinaryIntegerOperation(op, leftInt, rightInt),
            (Object.String leftStr, Object.String rightStr) => ExecuteBinaryStringOperation(op, leftStr, rightStr),
            _ => $"ERROR: Unsupported types for binary operation: {left.GetType()} {right.GetType()}",
        };
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

    private string? ExecuteBinaryStringOperation(Opcode op, Object.String left, Object.String right)
    {
        if (op != Opcode.Add)
        {
            return $"ERROR: unknown string operator: {op}";
        }

        var result = left.Value + right.Value;
        return Push(new Object.String(result));
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

    private Object.Array BuildArray(int startIndex, int endIndex)
    {
        var builder = ImmutableList<IObject>.Empty.ToBuilder();

        for (var i = startIndex; i < endIndex; i++)
        {
            builder.Add(_stack[i]);
        }

        return new(builder.ToImmutable());
    }

    private (Hash?, string?) BuildHash(int startIndex, int endIndex)
    {
        var builder = ImmutableDictionary<IHashable, IObject>.Empty.ToBuilder();

        for (var i = startIndex; i < endIndex; i += 2)
        {
            var key = _stack[i];
            var value = _stack[i + 1];

            if (key is not IHashable hashKey)
            {
                return (null, $"ERROR: unusable as hash key {key.GetType()}");
            }

            builder.Add(hashKey, value);
        }

        return (new(builder.ToImmutable()), null);
    }

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
