using System;
using System.Collections.Generic;
using System.IO;
using Monkey;

var monkeyFace = @"            __,__
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

Console.WriteLine($"Hello {System.Environment.UserName}! This is the Monkey Programming Language!");
Console.WriteLine("Feel free to type in commands");
Console.WriteLine("Enter <CTRL + D> to exit");

Start(Console.In, Console.Out);

void Start(TextReader inputReader, TextWriter outputWriter)
{
    var environment = new Monkey.Environment();
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
            PrintParserErrors(outputWriter, errors);
            continue;
        }

        var result = Evaluator.Eval(program, environment);
        if (result is not null)
        {
            outputWriter.WriteLine(result.Inspect());
        }
    }
}

void PrintParserErrors(TextWriter outputWriter, IEnumerable<string> errors)
{
    outputWriter.WriteLine(monkeyFace);
    outputWriter.WriteLine("Woops! We ran into some monkey business here!");
    outputWriter.WriteLine(" parser errors:");
    foreach (var error in errors)
    {
        outputWriter.WriteLine($"\t{error}");
    }
}
