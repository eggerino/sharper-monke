using System.IO;

namespace Monkey.Interpreter;

public static class Repl
{
    public static void Run(TextReader inputReader, TextWriter outputWriter, bool useVm = true)
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

            if (useVm)
            {
                EvaluateVm(program, outputWriter);
            }
            else
            {
                EvaluateInterpreter(program, environment, macroEnvironment, outputWriter);
            }
        }
    }

    private static void EvaluateInterpreter(Ast.Program program, Environment environment, Environment macroEnvironment, TextWriter outputWriter)
    {
        program = MacroExpansion.DefineMacros(program, macroEnvironment);
        var expanded = MacroExpansion.ExpandMacros(program, macroEnvironment);

        var result = Evaluator.Eval(expanded, environment);
        outputWriter.WriteLine(result.Inspect());
    }

    private static void EvaluateVm(Ast.Program program, TextWriter outputWriter)
    {
        var compiler = new Compiler();
        var error = compiler.Compile(program);
        if (error is not null)
        {
            outputWriter.WriteLine("Woops! Compilation failed:");
            outputWriter.WriteLine(error);
            return;
        }

        var machine = new Vm(compiler.GetByteCode());
        error = machine.Run();
        if (error is not null)
        {
            outputWriter.WriteLine("Woops! Executing bytecode failed:");
            outputWriter.WriteLine(error);
            return;
        }

        var stackTop = machine.GetStackTop();
        outputWriter.WriteLine(stackTop?.Inspect());
    }
}
