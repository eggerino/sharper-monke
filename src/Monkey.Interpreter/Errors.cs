using System.Collections.Generic;
using System.IO;

namespace Monkey.Interpreter;

public static class Errors
{
    private static string _monkeyFace = @"            __,__
   .--.  .-""     ""-.  .--.
  / .. \/  .-. .-.  \/ .. \
 | |  '|  /   Y   \  |'  | |
 | \   \  \ 0 | 0 /  /   / |
  \ '- ,\.-""""""""""""""-./, -' /
   ''-' /_   ^ ^   _\ '-''
       |  \._   _./  |
       \   \ '~' /   /
        '._ '-=-' _.'
           '-----'
";

    private static void PrintHeader(TextWriter outputWriter)
    {
        outputWriter.WriteLine(_monkeyFace);
        outputWriter.WriteLine("Woops! We ran into some monkey business here!");
    }

    public static void PrintParserErrors(TextWriter outputWriter, IEnumerable<string> errors)
    {
        PrintHeader(outputWriter);
        outputWriter.WriteLine(" Parser errors:");
        foreach (var error in errors)
        {
            outputWriter.Write("\t");
            outputWriter.WriteLine(error);
        }
    }

    public static void PrintCompilerError(TextWriter outputWriter, string error)
    {
        PrintHeader(outputWriter);
        outputWriter.WriteLine(" Compiler error:");
        outputWriter.Write("\t");
        outputWriter.WriteLine(error);
    }

    public static void PrintVmError(TextWriter outputWriter, string error)
    {
        PrintHeader(outputWriter);
        outputWriter.WriteLine(" Virtual machine error:");
        outputWriter.Write("\t");
        outputWriter.WriteLine(error);
    }
}
