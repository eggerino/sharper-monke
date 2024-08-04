using System;
using Monkey;

var input = """
let fibonacci = fn(x) {
  if (x == 0) {
    0
  } else {
    if (x == 1) {
      return 1;
    } else {
      fibonacci(x - 1) + fibonacci(x - 2);
    }
  }
};
puts(fibonacci(35));
""";

TimeSpan duration;

var useVm = args.Length > 0 && args[0] == "vm";

var lexer = new Lexer(input);
var parser = new Parser(lexer);
var (program, _) = parser.ParseProgram();

if (useVm)
{
    var compiler = new Compiler();
    compiler.Compile(program);

    var machine = new Vm(compiler.GetByteCode());

    var start = DateTime.Now;
    machine.Run();
    var end = DateTime.Now;

    duration = end - start;
}
else
{
    var start = DateTime.Now;
    Evaluator.Eval(program, new());
    var end = DateTime.Now;

    duration = end - start;
}

Console.Write("enigne=");
Console.WriteLine(useVm ? "vm" : "eval");


Console.Write("duration=");
Console.WriteLine(duration);
