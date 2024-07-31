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
    [InlineData("-5", -5)]
    [InlineData("-10", -10)]
    [InlineData("-50 + 100 + -50", 0)]
    [InlineData("(5 + 10 * 2 + 15 / 3) * 2 + -10", 50)]
    public void TestIntegerArithmetic(string input, object expected)
    {
        RunVmTests([new(input, expected)]);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("1 < 2", true)]
    [InlineData("1 > 2", false)]
    [InlineData("1 < 1", false)]
    [InlineData("1 > 1", false)]
    [InlineData("1 == 1", true)]
    [InlineData("1 != 1", false)]
    [InlineData("1 == 2", false)]
    [InlineData("1 != 2", true)]
    [InlineData("true == true", true)]
    [InlineData("false == false", true)]
    [InlineData("true == false", false)]
    [InlineData("true != false", true)]
    [InlineData("false != true", true)]
    [InlineData("(1 < 2) == true", true)]
    [InlineData("(1 < 2) == false", false)]
    [InlineData("(1 > 2) == true", false)]
    [InlineData("(1 > 2) == false", true)]
    [InlineData("!true", false)]
    [InlineData("!false", true)]
    [InlineData("!5", false)]
    [InlineData("!!true", true)]
    [InlineData("!!false", false)]
    [InlineData("!!5", true)]
    public void TestBooleanExpressions(string input, object expected)
    {
        RunVmTests([new(input, expected)]);
    }

    [Theory]
    [InlineData("if (true) { 10 }", 10)]
    [InlineData("if (true) { 10 } else { 20 }", 10)]
    [InlineData("if (false) { 10 } else { 20 } ", 20)]
    [InlineData("if (1) { 10 }", 10)]
    [InlineData("if (1 < 2) { 10 }", 10)]
    [InlineData("if (1 < 2) { 10 } else { 20 }", 10)]
    [InlineData("if (1 > 2) { 10 } else { 20 }", 20)]
    public void TestConditionals(string input, object expected)
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
            bool x => a => TestBooleanObject(x, a),
            _ => _ => Assert.Fail($"Unhandled test case for expected type {expected.GetType()}"),
        };

        testAction(actual);
    }

    private static void TestIntegerObject(long expected, IObject actual)
    {
        var actualInt = Assert.IsType<Integer>(actual);
        Assert.Equal(expected, actualInt.Value);
    }

    private static void TestBooleanObject(bool expected, IObject actual)
    {
        var actualBoolean = Assert.IsType<Object.Boolean>(actual);
        Assert.Equal(expected, actualBoolean.Value);
    }
}
