using System;
using System.Collections.Generic;
using System.Linq;
using Monkey.Ast;
using Monkey.Object;

namespace Monkey.Test;

public class VmTest
{
    record VmTestCase(string Input, object? Expected);

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
    [InlineData("!(if (false) { 5; })", true)]
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
    [InlineData("if (1 > 2) { 10 }", null)]
    [InlineData("if (false) { 10 }", null)]
    [InlineData("if ((if (false) { 10 })) { 10 } else { 20 }", 20)]
    public void TestConditionals(string input, object? expected)
    {
        RunVmTests([new(input, expected)]);
    }

    [Theory]
    [InlineData("let one = 1; one", 1)]
    [InlineData("let one = 1; let two = 2; one + two", 3)]
    [InlineData("let one = 1; let two = one + one; one + two", 3)]
    public void TestGlobalLetStatements(string input, object expected)
    {
        RunVmTests([new(input, expected)]);
    }

    [Theory]
    [InlineData(@"""monkey""", "monkey")]
    [InlineData(@"""mon"" + ""key""", "monkey")]
    [InlineData(@"""mon"" + ""key"" + ""banana""", "monkeybanana")]
    public void TestStringExpressions(string input, object expected)
    {
        RunVmTests([new(input, expected)]);
    }

    public static IEnumerable<object[]> TestArrayLiteralsData() => [
        ["[]", new int[0]],
        ["[1, 2, 3]", new int[3] {1, 2, 3}],
        ["[1 + 2, 3 * 4, 5 + 6]", new int[3] {3, 12, 11}],
    ];

    [Theory]
    [MemberData(nameof(TestArrayLiteralsData))]
    public void TestArrayLiterals(string input, object expected)
    {
        RunVmTests([new(input, expected)]);
    }

    public static IEnumerable<object[]> TestHashLiteralsData() => [
        ["{}", new Dictionary<IHashable, long>()],
        ["{1: 2, 2: 3}", new Dictionary<IHashable, long>()
        {
            {new Integer(1), 2},
            {new Integer(2), 3},
        }],
        ["{1 + 1: 2 * 2, 3 + 3: 4 * 4}", new Dictionary<IHashable, long>()
        {
            {new Integer(2), 4},
            {new Integer(6), 16},
        }],
    ];

    [Theory]
    [MemberData(nameof(TestHashLiteralsData))]
    public void TestHashLiterals(string input, object expected)
    {
        RunVmTests([new(input, expected)]);
    }

    [Theory]
    [InlineData("[1, 2, 3][1]", 2)]
    [InlineData("[1, 2, 3][0 + 2]", 3)]
    [InlineData("[[1, 1, 1]][0][0]", 1)]
    [InlineData("[][0]", null)]
    [InlineData("[1, 2, 3][99]", null)]
    [InlineData("[1][-1]", null)]
    [InlineData("{1: 1, 2: 2}[1]", 1)]
    [InlineData("{1: 1, 2: 2}[2]", 2)]
    [InlineData("{1: 1}[0]", null)]
    [InlineData("{}[0]", null)]
    public void TestIndexExpressions(string input, object? expected)
    {
        RunVmTests([new(input, expected)]);
    }

    [Theory]
    [InlineData("let fivePlusTen = fn() { 5 + 10 }; fivePlusTen();", 15)]
    [InlineData("let one = fn() { 1; }; let two = fn() { 2; }; one() + two();", 3)]
    [InlineData("let a = fn() { 1 }; let b = fn() { a() + 1 }; let c = fn() { b() + 1 }; c();", 3)]
    [InlineData("let earlyExit = fn() { return 99; 100; }; earlyExit();", 99)]
    [InlineData("let earlyExit = fn() { return 99; return 100; }; earlyExit();", 99)]
    public void TestCallingFunctionsWithoutArguments(string input, object expected)
    {
        RunVmTests([new(input, expected)]);
    }

    [Theory]
    [InlineData("let noReturn = fn() { }; noReturn();", null)]
    [InlineData("let noReturn = fn() { }; let noReturnTwo = fn() { noReturn(); }; noReturnTwo();", null)]
    public void TestFunctionsWitoutReturnValue(string input, object expected)
    {
        RunVmTests([new(input, expected)]);
    }

    [Theory]
    [InlineData("let returnsOne = fn() { 1; }; let returnsOneReturner = fn() { returnsOne; }; returnsOneReturner()();", 1)]
    public void TestFirstClassFunctions(string input, object expected)
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

    private static void TestExpectedObject(object? expected, IObject actual)
    {
        Action<IObject> testAction = expected switch
        {
            null => a => Assert.IsType<Null>(a),
            int x => a => TestIntegerObject(x, a),
            bool x => a => TestBooleanObject(x, a),
            string x => a => TestStringObject(x, a),
            int[] x => a => TestIntArrayObject(x, a),
            Dictionary<IHashable, long> x => a => TestIntHashObject(x, a),
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

    private static void TestStringObject(string expected, IObject actual)
    {
        var actualStr = Assert.IsType<Object.String>(actual);
        Assert.Equal(expected, actualStr.Value);
    }

    private static void TestIntArrayObject(int[] expected, IObject actual)
    {
        var actualArray = Assert.IsType<Object.Array>(actual);
        Assert.Equal(expected.Length, actualArray.Elements.Count);
        foreach (var (e, a) in expected.Zip(actualArray.Elements))
        {
            TestIntegerObject(e, a);
        }
    }

    private static void TestIntHashObject(Dictionary<IHashable, long> expected, IObject actual)
    {
        var actualHash = Assert.IsType<Hash>(actual);
        Assert.Equal(expected.Count, actualHash.Pairs.Count);

        foreach (var (eKey, eValue) in expected)
        {
            var aValue = actualHash.Pairs[eKey];
            TestIntegerObject(eValue, aValue);
        }
    }
}
