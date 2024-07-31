using System;
using System.Collections.Generic;
using System.Linq;
using Monkey.Code;
using Monkey.Object;

namespace Monkey;

public class Vm
{
    private const int StackSize = 2048;

    private readonly ArraySegment<byte> _instructions;
    private readonly IReadOnlyList<IObject> _constants;

    private readonly IObject[] _stack;
    private int _stackPointer;

    public Vm(ByteCode byteCode)
    {
        _instructions = byteCode.Instructions.ToArray().AsSegment();
        _constants = byteCode.Constants;

        _stack = new IObject[StackSize];
        _stackPointer = 0;
    }

    public IObject? GetStackTop() => _stackPointer switch
    {
        var x when x < 1 => null,
        _ => _stack[_stackPointer - 1],
    };

    public string? Run()
    {
        for (var ip = 0; ip < _instructions.Count; ip++)
        {
            var op = _instructions[ip].AsOpcode();

            switch (op)
            {
                case Opcode.Constant:
                    var constIndex = Instruction.ReadUint16(_instructions.Slice(ip + 1));
                    ip += 2;

                    var error = Push(_constants[constIndex]);
                    if (error is not null)
                    {
                        return error;
                    }
                    break;
            }
        }

        return null;
    }

    private string? Push(IObject value)
    {
        if (_stackPointer >= StackSize)
        {
            return "ERROR: stack overflow";
        }

        _stack[_stackPointer] = value;
        _stackPointer++;

        return null;
    }
}
