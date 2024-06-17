using System;
using Monkey.Interpreter;

Action action = args switch
{
[] => () => Repl.Run(Console.In, Console.Out),
[var filePath] => () => Interpreter.Execute(filePath),
    _ => () => Console.Error.WriteLine("Usage: <PROGRAMNAME> | <PROGRAMNAME> <FILEPATH>"),
};
action();
