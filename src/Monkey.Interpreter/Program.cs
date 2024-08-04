using System;
using Monkey.Interpreter;

Action action = args switch
{
    [] => () => Repl.Run(Console.In, Console.Out, true),
    ["--vm"] => () => Repl.Run(Console.In, Console.Out, true),
    ["--interpreter"] => () => Repl.Run(Console.In, Console.Out, false),
    [var filePath] => () => Interpreter.Execute(filePath, true),
    ["--vm", var filePath] => () => Interpreter.Execute(filePath, true),
    ["--interpreter", var filePath] => () => Interpreter.Execute(filePath, false),
    _ => () => Console.Error.WriteLine("Usage: <PROGRAMNAME> [FILEPATH] [--vm | --interpreter]"),
};
action();
