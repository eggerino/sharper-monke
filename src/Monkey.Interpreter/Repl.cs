using System.IO;

namespace Monkey.Interpreter;

public static class Repl
{
    public static void Run(TextReader inputReader, TextWriter outputWriter)
    {
        outputWriter.WriteLine($"Hello {System.Environment.UserName}! This is the Monkey Programming Language!");
        outputWriter.WriteLine("Feel free to type in commands");
        outputWriter.WriteLine("Enter <CTRL + D> to exit");

        var environment = new Environment();
        var macroEnvironment = new Environment();
        while (true)
        {
            outputWriter.Write(">> ");

            string? input;
            if ((input = inputReader.ReadLine()) is null)
            {
                outputWriter.WriteLine("\nExiting the REPL");
                return;
            }

            var lexer = new Lexer(input);
            var parser = new Parser(lexer);
            var (program, errors) = parser.ParseProgram();

            if (errors.Count > 0)
            {
                Errors.PrintParserErrors(outputWriter, errors);
                continue;
            }

            program = MacroExpansion.DefineMacros(program, macroEnvironment);
            var expanded = MacroExpansion.ExpandMacros(program, macroEnvironment);

            var result = Evaluator.Eval(expanded, environment);
            if (result is not null)
            {
                outputWriter.WriteLine(result.Inspect());
            }
        }
    }
}
