using System.Collections.Generic;
using System.IO;
using Monkey.Object;

namespace Monkey.Interpreter;

public static class Repl
{
    public static void Run(TextReader inputReader, TextWriter outputWriter, bool useVm)
    {
        outputWriter.WriteLine($"Hello {System.Environment.UserName}! This is the Monkey Programming Language!");
        outputWriter.WriteLine("Feel free to type in commands");
        outputWriter.WriteLine("Enter <CTRL + D> to exit");

        // VM State
        List<IObject>? constants = null;
        IObject[]? globals = null;
        SymbolTable? symbolTable = null;

        // Interpreter state
        Environment? environment = null;
        Environment? macroEnvironment = null;

        if (useVm)
        {
            outputWriter.WriteLine("Running in VM mode");
            constants = new();
            globals = Vm.CreateGlobalsArray();
            symbolTable = new();
        }
        else
        {
            outputWriter.WriteLine("Running in Interpeter mode");
            environment = new Environment();
            macroEnvironment = new Environment();
        }

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
                EvaluateVm(program, constants!, globals!, symbolTable!, outputWriter);
            }
            else
            {
                EvaluateInterpreter(program, environment!, macroEnvironment!, outputWriter);
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

    private static void EvaluateVm(Ast.Program program, List<IObject> constants, IObject[] globals, SymbolTable symbolTable, TextWriter outputWriter)
    {
        var compiler = Compiler.NewWithState(symbolTable, constants);
        var error = compiler.Compile(program);
        if (error is not null)
        {
            Errors.PrintCompilerError(outputWriter, error);
            return;
        }

        var machine = Vm.NewWithGlobalStore(compiler.GetByteCode(), globals);
        error = machine.Run();
        if (error is not null)
        {
            Errors.PrintVmError(outputWriter, error);
            return;
        }

        var lastStackTop = machine.GetLastPoppedStackElement();
        outputWriter.WriteLine(lastStackTop?.Inspect());
    }
}
