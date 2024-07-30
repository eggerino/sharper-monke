using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Monkey.Code;

public enum Opcode : byte
{
    Constant,
}

public static class OpcodeExtensions
{
    public static byte AsByte(this Opcode op) => (byte)op;

    public static Opcode AsOpcode(this byte op) => (Opcode)op;
}

public record Definition(string Name, IReadOnlyList<int> OperandWidths)
{
    public static Definition? Of(Opcode op) => op switch
    {
        Opcode.Constant => new(op.ToString(), [2]),
        _ => null,
    };

    public (int[], int) ReadOperands(ReadOnlySpan<byte> ins)
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

    public static ushort ReadUint16(ReadOnlySpan<byte> inst)
    {
        Span<byte> buffer = stackalloc byte[2];
        buffer.Clear();

        inst.Slice(0, 2).CopyTo(buffer);

        if (BitConverter.IsLittleEndian)
        {
            buffer.Reverse();
        }

        return BitConverter.ToUInt16(buffer);
    }

    public static string Disassemble(this ReadOnlySpan<byte> ins)
    {
        var builder = new StringBuilder();

        var i = 0;
        while (i < ins.Length)
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
            1 => $"{def.Name} {operands[0]}",
            _ => $"ERROR: unhandled operandCount for {def.Name}\n",
        };
    }
}
