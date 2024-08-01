using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Monkey.Code;

public enum Opcode : byte
{
    Constant,
    Pop,

    Add,
    Sub,
    Mul,
    Div,

    True,
    False,

    Equal,
    NotEqual,
    GreaterThan,

    Minus,
    Bang,

    JumpNotTruthy,
    Jump,

    Null,

    GetGlobal,
    SetGlobal,

    Array,
}

public static class OpcodeExtensions
{
    public static byte AsByte(this Opcode op) => (byte)op;

    public static Opcode AsOpcode(this byte op) => (Opcode)op;
}

public record Definition(string Name, IReadOnlyList<int> OperandWidths)
{
    private static readonly Dictionary<Opcode, Definition> _lookUp = new()
    {
        {Opcode.Constant, new("OpConstant", [2])},
        {Opcode.Pop, new("OpPop", [])},
        {Opcode.Add, new("OpAdd", [])},
        {Opcode.Sub, new("OpSub", [])},
        {Opcode.Mul, new("OpMul", [])},
        {Opcode.Div, new("OpDiv", [])},
        {Opcode.True, new("OpTrue", [])},
        {Opcode.False, new("OpFalse", [])},
        {Opcode.Equal, new("OpEqual", [])},
        {Opcode.NotEqual, new("OpNotEqual", [])},
        {Opcode.GreaterThan, new("OpGreaterThan", [])},
        {Opcode.Minus, new("OpMinus", [])},
        {Opcode.Bang, new("OpBang", [])},
        {Opcode.JumpNotTruthy, new("OpJumpNotTruthy", [2])},
        {Opcode.Jump, new("OpJump", [2])},
        {Opcode.Null, new("OpNull", [])},
        {Opcode.GetGlobal, new("OpGetGlobal", [2])},
        {Opcode.SetGlobal, new("OpSetGlobal", [2])},
        {Opcode.Array, new("OpArray", [2])},
    };

    public static Definition? Of(Opcode op) => _lookUp.TryGetValue(op, out var def) switch
    {
        true => def,
        false => null,
    };

    public (int[], int) ReadOperands(ArraySegment<byte> ins)
    {
        var operands = new int[OperandWidths.Count];
        var offset = 0;

        for (var i = 0; i < OperandWidths.Count; i++)
        {
            var width = OperandWidths[i];

            operands[i] = width switch
            {
                2 => Instruction.ReadUint16(ins.Slice(offset)),
                _ => throw new NotImplementedException(),
            };

            offset += width;
        }

        return (operands, offset);
    }
}

public static class Instruction
{
    public static IEnumerable<byte> Make(Opcode op, params int[] operands) => Definition.Of(op) switch
    {
        null => [],
        Definition def => new[] { op.AsByte() }
            .Concat(operands
                .Zip(def.OperandWidths)
                .Cast<(int Operand, int Width)>()
                .SelectMany(x => ToBigEndian(x.Operand, x.Width))),
    };

    private static IEnumerable<byte> ToBigEndian(int value, int width)
    {
        var bytes = BitConverter.GetBytes(value).Take(width);

        return BitConverter.IsLittleEndian switch
        {
            true => bytes.Reverse(),
            false => bytes,
        };
    }

    public static ushort ReadUint16(ArraySegment<byte> inst)
    {
        Span<byte> buffer = [inst[0], inst[1]];

        if (BitConverter.IsLittleEndian)
        {
            buffer.Reverse();
        }

        return BitConverter.ToUInt16(buffer);
    }

    public static string Disassemble(this ArraySegment<byte> ins)
    {
        var builder = new StringBuilder();

        var i = 0;
        while (i < ins.Count)
        {
            var def = Definition.Of(ins[i].AsOpcode());

            if (def is null)
            {
                builder.AppendLine($"ERRER: No definition of {ins[i].AsOpcode()} found");
                i++;
                continue;
            }

            var (operands, read) = def.ReadOperands(ins.Slice(i + 1));

            builder.AppendLine($"{i.ToString("D4")} {FormatInstruction(def, operands)}");

            i += 1 + read;
        }

        return builder.ToString();
    }

    private static string FormatInstruction(Definition def, int[] operands)
    {
        var operandCount = def.OperandWidths.Count;

        if (operandCount != operands.Length)
        {
            return $"ERROR: operand len {operands.Length} does not match defined {operandCount}\n";
        }

        return operandCount switch
        {
            0 => def.Name,
            1 => $"{def.Name} {operands[0]}",
            _ => $"ERROR: unhandled operandCount for {def.Name}\n",
        };
    }
}
