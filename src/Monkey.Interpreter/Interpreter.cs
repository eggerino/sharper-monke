using System.IO;

namespace Monkey.Interpreter;

public static class Interpreter
{
    public static void Execute(string filePath)
    {
        var source = File.ReadAllText(filePath);
        var lexer = new Lexer(source);
        var parser = new Parser(lexer);
        var (program, errors) = parser.ParseProgram();

        if (errors.Count > 0)
        {
            Errors.PrintParserErrors(System.Console.Out, errors);
        }
        else
        {
            var environment = new Environment();
            var macroEnvironment = new Environment();

            program = MacroExpansion.DefineMacros(program, macroEnvironment);
            var expanded = MacroExpansion.ExpandMacros(program, macroEnvironment);

            Evaluator.Eval(expanded, environment);
        }
    }
}
