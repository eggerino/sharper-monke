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
            (Opcode.Constant, new[] {65534}, new[] {Opcode.Constant.AsByte(), (byte)255, (byte)254}),
            (Opcode.Pop, new int[] {}, new[] {Opcode.Pop.AsByte()}),
            (Opcode.Add, new int[] {}, new[] {Opcode.Add.AsByte()}),
            (Opcode.Sub, new int[] {}, new[] {Opcode.Sub.AsByte()}),
            (Opcode.Mul, new int[] {}, new[] {Opcode.Mul.AsByte()}),
            (Opcode.Div, new int[] {}, new[] {Opcode.Div.AsByte()}),
            (Opcode.True, new int[] {}, new[] {Opcode.True.AsByte()}),
            (Opcode.False, new int[] {}, new[] {Opcode.False.AsByte()}),
            (Opcode.Equal, new int[] {}, new[] {Opcode.Equal.AsByte()}),
            (Opcode.NotEqual, new int[] {}, new[] {Opcode.NotEqual.AsByte()}),
            (Opcode.GreaterThan, new int[] {}, new[] {Opcode.GreaterThan.AsByte()}),
            (Opcode.Minus, new int[] {}, new[] {Opcode.Minus.AsByte()}),
            (Opcode.Bang, new int[] {}, new[] {Opcode.Bang.AsByte()}),
            (Opcode.JumpNotTruthy, new int[] {65534}, new[] {Opcode.JumpNotTruthy.AsByte(), (byte)255, (byte)254}),
            (Opcode.Jump, new int[] {65534}, new[] {Opcode.Jump.AsByte(), (byte)255, (byte)254}),
            (Opcode.Null, new int[] {}, new[] {Opcode.Null.AsByte()}),
            (Opcode.GetGlobal, new int[] {65534}, new[] {Opcode.GetGlobal.AsByte(), (byte)255, (byte)254}),
            (Opcode.SetGlobal, new int[] {65534}, new[] {Opcode.SetGlobal.AsByte(), (byte)255, (byte)254}),
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
            Instruction.Make(Opcode.Constant, 2),
            Instruction.Make(Opcode.Constant, 65535),
            Instruction.Make(Opcode.Pop),
            Instruction.Make(Opcode.Add),
            Instruction.Make(Opcode.Sub),
            Instruction.Make(Opcode.Mul),
            Instruction.Make(Opcode.Div),
            Instruction.Make(Opcode.True),
            Instruction.Make(Opcode.False),
            Instruction.Make(Opcode.Equal),
            Instruction.Make(Opcode.NotEqual),
            Instruction.Make(Opcode.GreaterThan),
            Instruction.Make(Opcode.Minus),
            Instruction.Make(Opcode.Bang),
            Instruction.Make(Opcode.JumpNotTruthy, 2),
            Instruction.Make(Opcode.JumpNotTruthy, 65535),
            Instruction.Make(Opcode.Jump, 2),
            Instruction.Make(Opcode.Jump, 65535),
            Instruction.Make(Opcode.Null),
            Instruction.Make(Opcode.GetGlobal, 65535),
            Instruction.Make(Opcode.SetGlobal, 65535),
        };
        var concatted = instructions.SelectMany(x => x).ToArray();

        var expected = @"0000 OpConstant 2
0003 OpConstant 65535
0006 OpPop
0007 OpAdd
0008 OpSub
0009 OpMul
0010 OpDiv
0011 OpTrue
0012 OpFalse
0013 OpEqual
0014 OpNotEqual
0015 OpGreaterThan
0016 OpMinus
0017 OpBang
0018 OpJumpNotTruthy 2
0021 OpJumpNotTruthy 65535
0024 OpJump 2
0027 OpJump 65535
0030 OpNull
0031 OpGetGlobal 65535
0034 OpSetGlobal 65535
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
