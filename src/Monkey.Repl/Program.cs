using System;
using System.IO;
using Monkey;

Console.WriteLine($"Hello {Environment.UserName}! This is the Monkey Programming Language!");
Console.WriteLine("Feel free to type in commands");

Start(Console.In, Console.Out);

static void Start(TextReader inputReader, TextWriter outputWriter)
{
    outputWriter.Write(">> ");
    string? input;
    while ((input = inputReader.ReadLine()) is not null)
    {
        var lexer = Lexer.For(input);
        foreach (var token in lexer.GetTokens())
        {
            outputWriter.WriteLine(token);
        }

        outputWriter.Write(">> ");
    }
    outputWriter.WriteLine("\nExiting the REPL");
}
