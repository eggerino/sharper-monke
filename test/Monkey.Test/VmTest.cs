using System;
using System.Collections.Generic;
using System.Linq;
using Monkey.Ast;
using Monkey.Code;
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
    [InlineData("""
    let returnsOne = fn() { 1; };
    let returnsOneReturner = fn() { returnsOne; };
    returnsOneReturner()();
    """, 1)]
    [InlineData("""
    let returnsOneReturner = fn() {
        let returnsOne = fn() { 1; };
        returnsOne;
    };
    returnsOneReturner()();
    """, 1)]
    public void TestFirstClassFunctions(string input, object expected)
    {
        RunVmTests([new(input, expected)]);
    }

    [Theory]
    [InlineData("""
    let one = fn() {
        let one = 1;
        one
    };
    one();
    """, 1)]
    [InlineData("""
    let oneAndTwo = fn() {
        let one = 1;
        let two = 2;
        one + two
    };
    oneAndTwo();
    """, 3)]
    [InlineData("""
    let oneAndTwo = fn() {
        let one = 1;
        let two = 2;
        one + two
    };
    let threeAndFour = fn() {
        let three = 3;
        let four = 4;
        three + four
    };
    oneAndTwo() + threeAndFour();
    """, 10)]
    [InlineData("""
    let firstFoobar = fn() {
        let foobar = 50;
        foobar;
    };
    let secondFoobar = fn() {
        let foobar = 100;
        foobar;
    };
    firstFoobar() + secondFoobar();
    """, 150)]
    [InlineData("""
    let globalSeed = 50;
    let minusOne = fn() {
        let num = 1;
        globalSeed - num;
    };
    let minusTwo = fn() {
        let num = 2;
        globalSeed - num;
    };
    minusOne() + minusTwo();
    """, 97)]
    public void TestCallingFunctionsWithBindings(string input, object expected)
    {
        RunVmTests([new(input, expected)]);
    }

    [Theory]
    [InlineData("""
    let identity = fn(a) { a; };
    identity(4);
    """, 4)]
    [InlineData("""
    let sum = fn(a, b) { a + b; };
    sum(1, 2);
    """, 3)]
    [InlineData("""
    let sum = fn(a, b) {
        let c = a + b;
        c;
    };
    sum(1, 2);
    """, 3)]
    [InlineData("""
    let sum = fn(a, b) {
        let c = a + b;
        c;
    };
    sum(1, 2) + sum(3, 4);
    """, 10)]
    [InlineData("""
    let sum = fn(a, b) {
        let c = a + b;
        c;
    };
    let outer = fn() {
        sum(1, 2) + sum(3, 4);
    };
    outer();
    """, 10)]
    [InlineData("""
    let globalNum = 10;

    let sum = fn(a, b) {
        let c = a + b;
        c + globalNum;
    };

    let outer = fn() {
        sum(1, 2) + sum(3, 4) + globalNum;
    };

    outer() + globalNum;
    """, 50)]
    public void TestCallingFunctionsWithArgumentsAndBindings(string input, object expected)
    {
        RunVmTests([new(input, expected)]);
    }

    [Fact]
    public void TestBuiltinFunctions()
    {
        RunVmTests([
            new(Input: "len(\"\")", 0),
            new(Input: "len(\"four\")", 4),
            new(Input: "len(\"Hello World\")", 11),
            new(Input: "len(1)", new Error("argument to `len` not supported, got Integer")),
            new(Input: "len(\"one\", \"two\")", new Error("wrong number of arguments. got=2, want=1")),
            new(Input: "len([1, 2, 3])", 3),
            new(Input: "len([])", 0),
            new(Input: "puts(\"hello\", \"world\")", null),
            new(Input: "first([1, 2, 3])", 1),
            new(Input: "first([])", null),
            new(Input: "first(1)", new Error("argument to `first` must be Array, got Integer")),
            new(Input: "last([1, 2, 3])", 3),
            new(Input: "last([])", null),
            new(Input: "last(1)", new Error("argument to `last` must be Array, got Integer")),
            new(Input: "rest([1, 2, 3])", new[] { 2, 3 }),
            new(Input: "rest([])", null),
            new(Input: "push([], 1)", new[] { 1 }),
            new(Input: "push(1, 1)", new Error("argument to `push` must be Array or Hash, got Integer")),

        ]);
    }

    [Theory]
    [InlineData("""
    let newClosure = fn(a) {
        fn() { a; };
    };
    let closure = newClosure(99);
    closure();
    """, 99)]
    [InlineData("""
    let newAdder = fn(a, b) {
        fn(c) { a + b + c };
    };
    let adder = newAdder(1, 2);
    adder(8);
    """, 11)]
    [InlineData("""
    let newAdder = fn(a, b) {
        let c = a + b;
        fn(d) { c + d };
    };
    let adder = newAdder(1, 2);
    adder(8);
    """, 11)]
    [InlineData("""
    let newAdderOuter = fn(a, b) {
        let c = a + b;
        fn(d) {
            let e = d + c;
            fn(f) { e + f; };
        };
    };
    let newAdderInner = newAdderOuter(1, 2)
    let adder = newAdderInner(3);
    adder(8);
    """, 14)]
    [InlineData("""
    let a = 1;
    let newAdderOuter = fn(b) {
        fn(c) {
            fn(d) { a + b + c + d };
        };
    };
    let newAdderInner = newAdderOuter(2)
    let adder = newAdderInner(3);
    adder(8);
    """, 14)]
    [InlineData("""
    let newClosure = fn(a, b) {
        let one = fn() { a; };
        let two = fn() { b; };
        fn() { one() + two(); };
    };
    let closure = newClosure(9, 90);
    closure();
    """, 99)]
    public void TestClosures(string input, object expected)
    {
        RunVmTests([new(input, expected)]);
    }

    [Theory]
    [InlineData("""
    let countDown = fn(x) {
        if (x == 0) {
            return 0;
        } else {
            countDown(x - 1);
        }
    };
    countDown(1);    
    """, 0)]
    [InlineData("""
    let countDown = fn(x) {
        if (x == 0) {
            return 0;
        } else {
            countDown(x - 1);
        }
    };
    let wrapper = fn() {
        countDown(1);
    };
    wrapper();
    """, 0)]
    [InlineData("""
    let wrapper = fn() {
        let countDown = fn(x) {
            if (x == 0) {
                return 0;
            } else {
                countDown(x - 1);
            }
        };
        countDown(1);
    };
    wrapper();
    """, 0)]
    public void TestRecursiveFunctions(string input, object expected)
    {
        RunVmTests([new(input, expected)]);
    }

    [Fact]
    public void TestRecursiveFibonacci()
    {
        RunVmTests([new("""
            let fibonacci = fn(x) {
                if (x == 0) {
                    return 0;
                } else {
                    if (x == 1) {
                        return 1;
                    } else {
                        fibonacci(x - 1) + fibonacci(x - 2);
                    }
                }
            };
            fibonacci(15);
            """, 610)]);
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

    [Theory]
    [InlineData("fn() { 1; }(1);", "ERROR: wrong number of arguments: want=0, got=1")]
    [InlineData("fn(a) { a; }();", "ERROR: wrong number of arguments: want=1, got=0")]
    [InlineData("fn(a, b) { a + b; }(1);", "ERROR: wrong number of arguments: want=2, got=1")]
    public void TestCallingFunctionWithWrongArguments(string input, string expected)
    {
        var program = Parse(input);
        var compiler = new Compiler();
        var error = compiler.Compile(program);
        Assert.Null(error);

        var vm = new Vm(compiler.GetByteCode());
        error = vm.Run();
        Assert.Equal(expected, error);
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
            Error x => a => TestErrorObject(x, a),
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

    private static void TestErrorObject(Error expected, IObject actual)
    {
        var actualError = Assert.IsType<Error>(actual);
        Assert.Equal(expected, actualError);
    }
}
