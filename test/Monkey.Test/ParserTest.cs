using System.Linq;
using Monkey.Ast;

namespace Monkey.Test;

public class ParserTest
{
    [Fact]
    public void TestLetStatements()
    {
        var input = @"
let x = 5;
let y = 10;
let foobar = 838383;
";
        string[] expectedIdentifiers = ["x", "y", "foobar"];

        var lexer = new Lexer(input);
        using var parser = new Parser(lexer);
        var program = parser.ParseProgram();
        CheckParserErrors(parser);

        Assert.Equal(expectedIdentifiers.Length, program.Statements.Count);

        foreach (var (statement, name) in program.Statements.Zip(expectedIdentifiers))
        {
            TestLetStatement(statement, name);
        }
    }

    private void TestLetStatement(IStatement statement, string name)
    {
        Assert.Equal("let", statement.GetTokenLiteral());

        Assert.IsType<LetStatement>(statement);
        LetStatement letStatement = (LetStatement)statement;

        Assert.Equal(name, letStatement.Name.Value);
        Assert.Equal(name, letStatement.Name.GetTokenLiteral());
    }

    private void CheckParserErrors(Parser parser)
    {
        var errors = parser.Errors;
        if (errors.Count == 0)
        {
            return;
        }

        Assert.Fail("\n\t- " + string.Join("\n\t- ", errors));
    }
}
