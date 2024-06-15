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

    private static IObject TestEval(string input)
    {
        var lexer = new Lexer(input);
        var parser = new Parser(lexer);
        var (program, errors) = parser.ParseProgram();

        Assert.Empty(errors);

        return Evaluator.Eval(program, new());
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

    private static void TestIntegerObject(IObject obj, long expected)
    {
        var result = Assert.IsType<Integer>(obj);
        Assert.Equal(expected, result.Value);
    }

    private static void TestBooleanObject(IObject obj, bool expected)
    {
        var result = Assert.IsType<Boolean>(obj);
        Assert.Equal(expected, result.Value);
    }

    private static void TestNullObject(IObject obj)
    {
        Assert.Equal(ObjectType.Null, obj.GetObjectType());
    }
}
