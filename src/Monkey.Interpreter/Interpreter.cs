using System.IO;

namespace Monkey.Interpreter;

public static class Interpreter
{
    public static void Execute(string filePath, bool useVm)
    {
        var source = File.ReadAllText(filePath);
        var lexer = new Lexer(source);
        var parser = new Parser(lexer);
        var (program, errors) = parser.ParseProgram();

        if (errors.Count > 0)
        {
            Errors.PrintParserErrors(System.Console.Error, errors);
            System.Environment.Exit(1);
        }

        if (useVm)
        {
            ExecuteVm(program);
        }
        else
        {
            ExecuteInterpreter(program);
        }
    }

    public static void ExecuteInterpreter(Ast.Program program)
    {
        var environment = new Environment();
        var macroEnvironment = new Environment();

        program = MacroExpansion.DefineMacros(program, macroEnvironment);
        var expanded = MacroExpansion.ExpandMacros(program, macroEnvironment);

        Evaluator.Eval(expanded, environment);
    }

    public static void ExecuteVm(Ast.Program program)
    {
        var compiler = new Compiler();
        var error = compiler.Compile(program);
        if (error is not null)
        {
            Errors.PrintCompilerError(System.Console.Error, error);
            System.Environment.Exit(1);
        }

        var vm = new Vm(compiler.GetByteCode());
        error = vm.Run();
        if (error is not null)
        {
            Errors.PrintVmError(System.Console.Error, error);
            System.Environment.Exit(1);
        }
    }
}
