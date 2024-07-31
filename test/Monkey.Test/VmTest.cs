using System;
using System.Collections.Generic;
using Monkey.Ast;
using Monkey.Object;

namespace Monkey.Test;

public class VmTest
{
    record VmTestCase(string Input, object Expected);

    [Theory]
    [InlineData("1", 1)]
    [InlineData("2", 2)]
    [InlineData("1 + 2", 3)]
    [InlineData("1 - 2", -1)]
    [InlineData("1 * 2", 2)]
    [InlineData("4 / 2", 2)]
    [InlineData("50 / 2 * 2 + 10 - 5", 55)]
    [InlineData("5 + 5 + 5 + 5 - 10", 10)]
    [InlineData("2 * 2 * 2 * 2 * 2", 32)]
    [InlineData("5 * 2 + 10", 20)]
    [InlineData("5 + 2 * 10", 25)]
    [InlineData("5 * (2 + 10)", 60)]
    public void TestIntegerArithmetic(string input, object expected)
    {
        RunVmTests([new(input, expected)]);
    }

    private static void RunVmTests(IEnumerable<VmTestCase> tests)
    {
        foreach (var test in tests)
        {
            var program = Parse(test.Input);

            var compiler = new Compiler();

            var error = compiler.Compile(program);
            Assert.Null(error);

            var vm = new Vm(compiler.GetByteCode());
            error = vm.Run();
            Assert.Null(error);

            var stackElem = vm.GetLastPoppedStackElement();

            TestExpectedObject(test.Expected, stackElem);
        }
    }

    private static Program Parse(string input)
    {
        var lexer = new Lexer(input);
        var parser = new Parser(lexer);
        var (program, _) = parser.ParseProgram();
        return program;
    }

    private static void TestExpectedObject(object expected, IObject actual)
    {
        Action<IObject> testAction = expected switch
        {
            int x => a => TestIntegerObject(x, a),
            _ => _ => Assert.Fail($"Unhandled test case for expected type {expected.GetType()}"),
        };

        testAction(actual);
    }

    private static void TestIntegerObject(long expected, IObject actual)
    {
        var actualInt = Assert.IsType<Integer>(actual);
        Assert.Equal(expected, actualInt.Value);
    }
}
