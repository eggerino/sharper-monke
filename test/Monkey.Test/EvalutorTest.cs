using Monkey.Object;

namespace Monkey.Test;

public class EvaluatorTest
{
    [Theory]
    [InlineData("5", 5L)]
    [InlineData("10", 10L)]
    public void TestEvalIntegerExpression(string input, long expected)
    {
        var evaluated = TestEval(input);
        TestIntegerObject(evaluated, expected);
    }

    private static IObject? TestEval(string input)
    {
        var lexer = new Lexer(input);
        var parser = new Parser(lexer);
        var (program, errors) = parser.ParseProgram();

        Assert.Empty(errors);

        return Evaluator.Eval(program);
    }

    private static void TestIntegerObject(IObject? obj, long expected)
    {
        var result = Assert.IsType<Integer>(obj);
        Assert.Equal(expected, result.Value);
    }
}
