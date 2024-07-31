using System;
using System.Linq;
using Monkey.Code;

namespace Monkey.Test;

public class CodeTest
{
    [Fact]
    public void TestMake()
    {
        var tests = new[]
        {
            (Opcode.Constant, new[] {65534}, new[]{Opcode.Constant.AsByte(), (byte)255, (byte)254}),
            (Opcode.Add, new int[]{}, new[]{Opcode.Add.AsByte()}),
        };

        foreach (var (op, operands, expected) in tests)
        {
            var instruction = Instruction.Make(op, operands);

            Assert.Equal(expected, instruction);
        }
    }

    [Fact]
    public void TestInstructionsString()
    {
        var instructions = new[]
        {
            Instruction.Make(Opcode.Add),
            Instruction.Make(Opcode.Constant, 2),
            Instruction.Make(Opcode.Constant, 65535),
        };
        var concatted = instructions.SelectMany(x => x).ToArray();

        var expected = @"0000 OpAdd
0001 OpConstant 2
0004 OpConstant 65535
";

        var actual = concatted.AsSegment().Disassemble();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(Opcode.Constant, 2, 65535)]
    public void TestReadOperands(Opcode op, int bytesRead, params int[] operands)
    {
        var instruction = Instruction.Make(op, operands).ToArray().AsSegment();
        var definition = Definition.Of(op);

        Assert.NotNull(definition);

        var (operandsRead, n) = definition.ReadOperands(instruction.Slice(1));

        Assert.Equal(bytesRead, n);
        Assert.Equal(operands, operandsRead);
    }
}
