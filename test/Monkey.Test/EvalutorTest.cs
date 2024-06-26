using System.Collections.Generic;
using System.Linq;
using Monkey.Ast;
using Monkey.Object;

namespace Monkey.Test;

public class EvaluatorTest
{
    [Theory]
    [InlineData("5", 5L)]
    [InlineData("10", 10L)]
    [InlineData("-5", -5L)]
    [InlineData("-10", -10L)]
    [InlineData("5 + 5 + 5 + 5 - 10", 10L)]
    [InlineData("2 * 2 * 2 * 2 * 2", 32L)]
    [InlineData("-50 + 100 + -50", 0L)]
    [InlineData("5 * 2 + 10", 20L)]
    [InlineData("5 + 2 * 10", 25L)]
    [InlineData("20 + 2 * -10", 0L)]
    [InlineData("50 / 2 * 2 + 10", 60L)]
    [InlineData("2 * (5 + 10)", 30L)]
    [InlineData("3 * 3 * 3 + 10", 37L)]
    [InlineData("3 * (3 * 3) + 10", 37L)]
    [InlineData("(5 + 10 * 2 + 15 / 3) * 2 + -10", 50L)]
    public void TestEvalIntegerExpression(string input, long expected)
    {
        var evaluated = TestEval(input);
        TestIntegerObject(evaluated, expected);
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
    [InlineData(@"""hello"" == ""hello""", true)]
    [InlineData(@"""hello"" == ""world""", false)]
    [InlineData(@"""hello"" != ""hello""", false)]
    [InlineData(@"""hello"" != ""world""", true)]
    [InlineData(@"(""hello"" == ""hello"") == true", true)]
    public void TestEvalBooleanExpression(string input, bool expected)
    {
        var evaluated = TestEval(input);
        TestBooleanObject(evaluated, expected);
    }

    [Theory]
    [InlineData("!true", false)]
    [InlineData("!false", true)]
    [InlineData("!5", false)]
    [InlineData("!!true", true)]
    [InlineData("!!false", false)]
    [InlineData("!!5", true)]
    [InlineData(@"!!""hello world""", true)]
    public void TestBangOperator(string input, bool expected)
    {
        var evaluated = TestEval(input);
        TestBooleanObject(evaluated, expected);
    }

    [Theory]
    [InlineData("if (true) { 10 }", 10L)]
    [InlineData("if (false) { 10 }", null)]
    [InlineData("if (1) { 10 }", 10L)]
    [InlineData("if (1 < 2) { 10 }", 10L)]
    [InlineData("if (1 > 2) { 10 }", null)]
    [InlineData("if (1 > 2) { 10 } else { 20 }", 20L)]
    [InlineData("if (1 < 2) { 10 } else { 20 }", 10L)]
    public void TestIfElseExpressions(string input, long? expected)
    {
        var evaluated = TestEval(input);
        if (expected.HasValue)
        {
            TestIntegerObject(evaluated, expected.Value);
        }
        else
        {
            TestNullObject(evaluated);
        }
    }

    [Theory]
    [InlineData("return 10;", 10L)]
    [InlineData("return 10; 9;", 10L)]
    [InlineData("return 2 * 5; 9;", 10L)]
    [InlineData("9; return 2 * 5; 9;", 10L)]
    [InlineData(@"
if (10 > 1) {
  if (10 > 1) {
    return 10;
  }

  return 1;
}
", 10L)]
    public void TestReturnStatements(string input, long expected)
    {
        var evaluated = TestEval(input);
        TestIntegerObject(evaluated, expected);
    }

    [Theory]
    [InlineData("5 + true;", "type mismatch: Integer + Boolean")]
    [InlineData("5 + true; 5;", "type mismatch: Integer + Boolean")]
    [InlineData("-true", "unknown operator: -Boolean")]
    [InlineData("true + false;", "unknown operator: Boolean + Boolean")]
    [InlineData("5; true + false; 5", "unknown operator: Boolean + Boolean")]
    [InlineData("if (10 > 1) { true + false; }", "unknown operator: Boolean + Boolean")]
    [InlineData(@"
if (10 > 1) {
  if (10 > 1) {
    return true + false;
  }

  return 1;
}
", "unknown operator: Boolean + Boolean")]
    [InlineData("foobar", "identifier not found: foobar")]
    [InlineData(@"""Hello"" - ""World""", "unknown operator: String - String")]
    [InlineData(@"{""name"": ""Monkey""}[fn(x) { x }];", "unusable as hash key: Function")]
    public void TestErrorHandling(string input, string message)
    {
        var evaluated = TestEval(input);
        var error = Assert.IsType<Error>(evaluated);
        Assert.Equal(message, error.Message);
    }

    [Theory]
    [InlineData("let a = 5; a;", 5L)]
    [InlineData("let a = 5 * 5; a;", 25L)]
    [InlineData("let a = 5; let b = a; b;", 5L)]
    [InlineData("let a = 5; let b = a; let c = a + b + 5; c;", 15L)]
    public void TestLetStatements(string input, long expected)
    {
        var evaluated = TestEval(input);
        TestIntegerObject(evaluated, expected);
    }

    [Fact]
    public void TestFunctionObject()
    {
        var input = "fn(x) { x + 2; };";

        var evaluated = TestEval(input);
        var function = Assert.IsType<Function>(evaluated);
        var parameter = Assert.Single(function.Parameters);
        Assert.Equal("x", parameter.GetDebugString());
        Assert.Equal("(x + 2)", function.Body.GetDebugString());
    }

    [Theory]
    [InlineData("let identity = fn(x) { x; }; identity(5);", 5)]
    [InlineData("let identity = fn(x) { return x; }; identity(5);", 5)]
    [InlineData("let double = fn(x) { x * 2; }; double(5);", 10)]
    [InlineData("let add = fn(x, y) { x + y; }; add(5, 5);", 10)]
    [InlineData("let add = fn(x, y) { x + y; }; add(5 + 5, add(5, 5));", 20)]
    [InlineData("fn(x) { x; }(5)", 5)]
    public void TestFunctionApplication(string input, long expected)
    {
        var evaluated = TestEval(input);
        TestIntegerObject(evaluated, expected);
    }

    [Fact]
    public void TestClosures()
    {
        var input = @"
let newAdder = fn(x) {
  fn(y) { x + y };
};

let addTwo = newAdder(2);
addTwo(2);";

        var evaluated = TestEval(input);
        TestIntegerObject(evaluated, 4L);
    }

    [Fact]
    public void TestStringLiteral()
    {
        var input = @"""Hello, World!""";

        var evaluated = TestEval(input);
        var str = Assert.IsType<String>(evaluated);
        Assert.Equal("Hello, World!", str.Value);
    }

    [Fact]
    public void TestStringConcatenation()
    {
        var input = @"""Hello"" + "" "" + ""World!""";

        var evaluated = TestEval(input);
        var str = Assert.IsType<String>(evaluated);
        Assert.Equal("Hello World!", str.Value);
    }

    [Theory]
    [InlineData(@"len("""")", 0L)]
    [InlineData(@"len(""four"")", 4L)]
    [InlineData(@"len(""hello world"")", 11L)]
    [InlineData(@"len(1)", "argument to `len` not supported, got Integer")]
    [InlineData(@"len(""one"", ""two"")", "wrong number of arguments. got=2, want=1")]
    [InlineData(@"len([1, 2, 3])", 3L)]
    [InlineData(@"len([])", 0L)]
    [InlineData(@"puts(""hello"", ""world!"")", null)]
    [InlineData(@"first([1, 2, 3])", 1L)]
    [InlineData(@"first([])", null)]
    [InlineData(@"first(1)", "argument to `first` must be Array, got Integer")]
    [InlineData(@"last([1, 2, 3])", 3L)]
    [InlineData(@"last([])", null)]
    [InlineData(@"last(1)", "argument to `last` must be Array, got Integer")]
    [InlineData(@"rest([1, 2, 3])", "array", 2L, 3L)]
    [InlineData(@"rest([])", null)]
    [InlineData(@"push([], 1)", "array", 1L)]
    [InlineData(@"push(1, 1)", "argument to `push` must be Array or Hash, got Integer")]
    public void TestBuiltinFunctions(string input, params object[] expected)
    {
        var evaluated = TestEval(input);
        System.Action check = expected switch
        {
            null => () => TestNullObject(evaluated),
            [long number] => () => TestIntegerObject(evaluated, number),
            ["array", .. var items] => () =>
            {
                var array = Assert.IsType<Array>(evaluated);
                Assert.Equal(items.Length, array.Elements.Count);
                foreach (var (expected, actual) in items.Zip(array.Elements))
                {
                    TestIntegerObject(actual, (long)expected);
                }

            }
            ,
            [string text] => () => Assert.Equal(text, Assert.IsType<Error>(evaluated).Message),
            _ => () => Assert.Fail($"expected type not supported by the test {expected.GetType()}"),
        };
        check();
    }

    [Fact]
    public void TestArrayLiterals()
    {
        var input = "[1, 2 * 2, 3 + 3]";

        var evaluated = TestEval(input);
        var array = Assert.IsType<Array>(evaluated);
        Assert.Equal(3, array.Elements.Count);
        TestIntegerObject(array.Elements[0], 1);
        TestIntegerObject(array.Elements[1], 4);
        TestIntegerObject(array.Elements[2], 6);
    }

    [Theory]
    [InlineData("[1, 2, 3][0]", 1L)]
    [InlineData("[1, 2, 3][1]", 2L)]
    [InlineData("[1, 2, 3][2]", 3L)]
    [InlineData("let i = 0; [1][i];", 1L)]
    [InlineData("[1, 2, 3][1 + 1];", 3L)]
    [InlineData("let myArray = [1, 2, 3]; myArray[2];", 3L)]
    [InlineData("let myArray = [1, 2, 3]; myArray[0] + myArray[1] + myArray[2];", 6L)]
    [InlineData("let myArray = [1, 2, 3]; let i = myArray[0]; myArray[i]", 2L)]
    [InlineData("[1, 2, 3][3]", null)]
    [InlineData("[1, 2, 3][-1]", null)]
    public void TestArrayIndexExpressions(string input, long? expected)
    {
        var evaluated = TestEval(input);
        if (expected.HasValue)
        {
            TestIntegerObject(evaluated, expected.Value);
        }
        else
        {
            TestNullObject(evaluated);
        }
    }

    [Fact]
    public void TestHashLiterals()
    {
        var input = @"let two = ""two"";
{
    ""one"": 10 - 9,
    two: 1 + 1,
    ""thr"" + ""ee"": 6 / 2,
    4: 4,
    true: 5,
    false: 6
}";
        var expected = new Dictionary<IHashable, long>
        {
            {new String("one"), 1},
            {new String("two"), 2},
            {new String("three"), 3},
            {new Integer(4), 4},
            {new Object.Boolean(true), 5},
            {new Object.Boolean(false), 6},
        };

        var evaluated = TestEval(input);
        var hash = Assert.IsType<Hash>(evaluated);
        Assert.Equal(expected.Count, hash.Pairs.Count);

        foreach (var (expectedKey, expectedValue) in expected)
        {
            var actual = hash.Pairs[expectedKey];
            TestIntegerObject(actual, expectedValue);
        }
    }

    [Theory]
    [InlineData(@"{""foo"": 5}[""foo""]", 5L)]
    [InlineData(@"{""foo"": 5}[""bar""]", null)]
    [InlineData(@"let key = ""foo""; {""foo"": 5}[key]", 5L)]
    [InlineData(@"{}[""foo""]", null)]
    [InlineData(@"{5: 5}[5]", 5L)]
    [InlineData(@"{true: 5}[true]", 5L)]
    [InlineData(@"{false: 5}[false]", 5L)]
    public void TestHashIndexExpressions(string input, long? expected)
    {
        var evaluated = TestEval(input);
        if (evaluated is Integer integer)
        {
            TestIntegerObject(integer, expected!.Value);
        }
        else
        {
            TestNullObject(evaluated);
        }
    }

    [Theory]
    [InlineData("quote(5)", "5")]
    [InlineData("quote(5 + 8)", "(5 + 8)")]
    [InlineData("quote(foobar)", "foobar")]
    [InlineData("quote(foobar + barfoo)", "(foobar + barfoo)")]
    [InlineData("quote(unquote(4))", "4")]
    [InlineData("quote(unquote(4 + 4))", "8")]
    [InlineData("quote(8 + unquote(4 + 4))", "(8 + 8)")]
    [InlineData("quote(unquote(4 + 4) + 8)", "(8 + 8)")]
    [InlineData("let foobar = 8; quote(foobar)", "foobar")]
    [InlineData("let foobar = 8; quote(unquote(foobar))", "8")]
    [InlineData("quote(unquote(true))", "true")]
    [InlineData("quote(unquote(true == false))", "false")]
    [InlineData("quote(unquote(quote(4 + 4)))", "(4 + 4)")]
    [InlineData("let quotedInfixExpression = quote(4 + 4); quote(unquote(4 + 4) + unquote(quotedInfixExpression))", "(8 + (4 + 4))")]
    public void TestQuoteUnquote(string input, string expected)
    {
        var evaluated = TestEval(input);

        var quote = Assert.IsType<Quote>(evaluated);
        Assert.NotNull(quote.Node);
        Assert.Equal(expected, quote.Node.GetDebugString());
    }

    [Fact]
    public void TestDefineMacros()
    {
        var input = "let number = 1; let function = fn(x, y) { x + y }; let mymacro = macro(x, y) { x + y; };";

        var program = TestParseProgram(input);
        var env = new Environment();

        program = MacroExpansion.DefineMacros(program, env);

        Assert.Equal(2, program.Statements.Count);
        Assert.Null(env.Get("number"));
        Assert.Null(env.Get("function"));
        var mymacro = env.Get("mymacro");
        var macro = Assert.IsType<Macro>(mymacro);
        Assert.Equal(2, macro.Parameters.Count);
        Assert.Equal("x", macro.Parameters[0].GetDebugString());
        Assert.Equal("y", macro.Parameters[1].GetDebugString());
        Assert.Equal("(x + y)", macro.Body.GetDebugString());
    }

    [Theory]
    [InlineData(@"
            let infixExpression = macro() { quote(1 + 2); };
            infixExpression();
            ", "(1 + 2)")]
    [InlineData(@"
            let reverse = macro(a, b) { quote(unquote(b) - unquote(a)); };
            reverse(2 + 2, 10 - 5);
            ", "(10 - 5) - (2 + 2)")]
    [InlineData(@"
            let unless = macro(condition, consequence, alternative) {
                quote(if (!(unquote(condition))) {
                    unquote(consequence);
                } else {
                    unquote(alternative);
                });
            };

            unless(10 > 5, puts(""not greater""), puts(""greater""));
            ",
            @"if (!(10 > 5)) { puts(""not greater"") } else { puts(""greater"") }")]
    public void TestExpandMacros(string input, string expected)
    {
        var program = TestParseProgram(input);
        var expectedProgram = TestParseProgram(expected);

        var env = new Environment();
        program = MacroExpansion.DefineMacros(program, env);
        var expanded = MacroExpansion.ExpandMacros(program, env);
        Assert.Equal(expectedProgram.GetDebugString(), expanded.GetDebugString());
    }

    private static Program TestParseProgram(string input)
    {
        var lexer = new Lexer(input);
        var parser = new Parser(lexer);
        var (program, errors) = parser.ParseProgram();

        Assert.Empty(errors);
        return program;
    }

    private static IObject TestEval(string input)
    {
        return Evaluator.Eval(TestParseProgram(input), new());
    }

    private static void TestIntegerObject(IObject obj, long expected)
    {
        var result = Assert.IsType<Integer>(obj);
        Assert.Equal(expected, result.Value);
    }

    private static void TestBooleanObject(IObject obj, bool expected)
    {
        var result = Assert.IsType<Object.Boolean>(obj);
        Assert.Equal(expected, result.Value);
    }

    private static void TestNullObject(IObject obj)
    {
        Assert.Equal(ObjectType.Null, obj.GetObjectType());
    }
}
