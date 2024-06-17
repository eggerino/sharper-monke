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

    public static void PrintParserErrors(TextWriter outputWriter, IEnumerable<string> errors)
    {
        outputWriter.WriteLine(_monkeyFace);
        outputWriter.WriteLine("Woops! We ran into some monkey business here!");
        outputWriter.WriteLine(" parser errors:");
        foreach (var error in errors)
        {
            outputWriter.WriteLine($"\t{error}");
        }
    }
}
