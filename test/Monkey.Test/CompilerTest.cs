using System;
using System.Collections.Generic;
using System.Linq;
using Monkey.Ast;
using Monkey.Code;
using Monkey.Object;

namespace Monkey.Test;

public class CompilerTest
{
    record CompilerTestCase(
        string Input,
        IReadOnlyList<object> ExpectedConstants,
        IEnumerable<IEnumerable<byte>> ExpectedInstructions);

    [Fact]
    public void TestIntegerArithmetic()
    {
        RunCompilerTests([
            new(Input: "1 + 2",
                ExpectedConstants: [1, 2],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.Constant, 1),
                    Instruction.Make(Opcode.Add),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "1; 2",
                ExpectedConstants: [1, 2],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.Pop),
                    Instruction.Make(Opcode.Constant, 1),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "1 - 2",
                ExpectedConstants: [1, 2],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.Constant, 1),
                    Instruction.Make(Opcode.Sub),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "1 * 2",
                ExpectedConstants: [1, 2],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.Constant, 1),
                    Instruction.Make(Opcode.Mul),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "2 / 1",
                ExpectedConstants: [2, 1],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.Constant, 1),
                    Instruction.Make(Opcode.Div),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "-1",
                ExpectedConstants: [1],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.Minus),
                    Instruction.Make(Opcode.Pop),
                ]),
        ]);
    }

    [Fact]
    public void TestBooleanExpressions()
    {
        RunCompilerTests([
            new(Input: "true",
                ExpectedConstants: [],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.True),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "false",
                ExpectedConstants: [],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.False),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "1 > 2",
                ExpectedConstants: [1, 2],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.Constant, 1),
                    Instruction.Make(Opcode.GreaterThan),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "1 < 2",
                ExpectedConstants: [2, 1],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.Constant, 1),
                    Instruction.Make(Opcode.GreaterThan),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "1 == 2",
                ExpectedConstants: [1, 2],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.Constant, 1),
                    Instruction.Make(Opcode.Equal),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "1 != 2",
                ExpectedConstants: [1, 2],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.Constant, 1),
                    Instruction.Make(Opcode.NotEqual),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "true == false",
                ExpectedConstants: [],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.True),
                    Instruction.Make(Opcode.False),
                    Instruction.Make(Opcode.Equal),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "true != false",
                ExpectedConstants: [],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.True),
                    Instruction.Make(Opcode.False),
                    Instruction.Make(Opcode.NotEqual),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "!true",
                ExpectedConstants: [],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.True),
                    Instruction.Make(Opcode.Bang),
                    Instruction.Make(Opcode.Pop),
                ]),
        ]);
    }

    [Fact]
    public void TestConditionals()
    {
        RunCompilerTests([
            new(Input: "if (true) { 10 }; 3333;",
                ExpectedConstants: [10, 3333],
                ExpectedInstructions:[
                    Instruction.Make(Opcode.True),                  // 0000
                    Instruction.Make(Opcode.JumpNotTruthy, 10),     // 0001
                    Instruction.Make(Opcode.Constant, 0),           // 0004
                    Instruction.Make(Opcode.Jump, 11),              // 0007
                    Instruction.Make(Opcode.Null),                  // 0010
                    Instruction.Make(Opcode.Pop),                   // 0011
                    Instruction.Make(Opcode.Constant, 1),           // 0012
                    Instruction.Make(Opcode.Pop),                   // 0015
                ]),
            new(Input: "if (true) { 10 } else { 20 }; 3333;",
                ExpectedConstants: [10, 20, 3333],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.True),                  // 0000
                    Instruction.Make(Opcode.JumpNotTruthy, 10),     // 0001
                    Instruction.Make(Opcode.Constant, 0),           // 0004
                    Instruction.Make(Opcode.Jump, 13),              // 0007
                    Instruction.Make(Opcode.Constant, 1),           // 0010
                    Instruction.Make(Opcode.Pop),                   // 0013
                    Instruction.Make(Opcode.Constant, 2),           // 0014
                    Instruction.Make(Opcode.Pop),                   // 0017
                ]),
        ]);
    }

    [Fact]
    public void TestGlobalLetStatements()
    {
        RunCompilerTests([
            new(Input: "let one = 1; let two = 2;",
                ExpectedConstants: [1, 2],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.SetGlobal, 0),
                    Instruction.Make(Opcode.Constant, 1),
                    Instruction.Make(Opcode.SetGlobal, 1),
                ]),
            new(Input: "let one = 1; one;",
                ExpectedConstants: [1],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.SetGlobal, 0),
                    Instruction.Make(Opcode.GetGlobal, 0),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "let one = 1; let two = one; two;",
                ExpectedConstants: [1],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.SetGlobal, 0),
                    Instruction.Make(Opcode.GetGlobal, 0),
                    Instruction.Make(Opcode.SetGlobal, 1),
                    Instruction.Make(Opcode.GetGlobal, 1),
                    Instruction.Make(Opcode.Pop),
                ]),
        ]);
    }

    private static void RunCompilerTests(IEnumerable<CompilerTestCase> tests)
    {
        foreach (var test in tests)
        {
            var program = Parse(test.Input);
            var compiler = new Compiler();

            var error = compiler.Compile(program);
            Assert.Null(error);

            var bytecode = compiler.GetByteCode();

            Assert.NotNull(bytecode);
            TestInstructions(test.ExpectedInstructions, bytecode.Instructions);
            TestConstants(test.ExpectedConstants, bytecode.Constants);
        }
    }

    private static Program Parse(string input)
    {
        var lexer = new Lexer(input);
        var parser = new Parser(lexer);
        var (program, _) = parser.ParseProgram();
        return program;
    }

    private static void TestInstructions(IEnumerable<IEnumerable<byte>> expected, IEnumerable<byte> actual)
    {
        var expectedAssembly = expected.SelectMany(x => x).ToArray().AsSegment().Disassemble();
        var actualAssembly = actual.ToArray().AsSegment().Disassemble();

        Assert.Equal(expectedAssembly, actualAssembly);
    }

    private static void TestConstants(IReadOnlyCollection<object> expected, IReadOnlyCollection<IObject> actual)
    {
        Assert.Equal(expected.Count, actual.Count);

        foreach (var (exp, act) in expected.Zip(actual))
        {
            if (exp is int expInt)
            {
                TestIntegerObject(expInt, act);
            }
            else
            {
                Assert.Fail("Unexpected variant");
            }
        }
    }

    private static void TestIntegerObject(long expected, IObject actual)
    {
        var actualInt = Assert.IsType<Integer>(actual);
        Assert.Equal(expected, actualInt.Value);
    }
}
