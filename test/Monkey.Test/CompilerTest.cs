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
                    Instruction.Make(Opcode.Constant, 1)
                ])
        ]);
    }

    private static void RunCompilerTests(IEnumerable<CompilerTestCase> tests)
    {
        foreach (var test in tests)
        {
            var program = Parse(test.Input);
            var compiler = new Compiler(program);

            var bytecode = compiler.Compile();

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
        var expectedAssembly = ((ReadOnlySpan<byte>)expected.SelectMany(x => x).ToArray().AsSpan()).Disassemble();
        var actualAssembly = ((ReadOnlySpan<byte>)actual.ToArray().AsSpan()).Disassemble();

        Assert.Equal(expectedAssembly, actualAssembly);
    }

    private static void TestConstants(IReadOnlyCollection<object> expected, IReadOnlyCollection<IObject> actual)
    {
        Assert.Equal(expected.Count, actual.Count);

        foreach (var (exp, act) in expected.Zip(actual))
        {
            if (exp is int)
            {
                TestIntegerObject((long)exp, act);
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