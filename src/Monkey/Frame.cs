using System;
using Monkey.Object;

namespace Monkey;

public class Frame
{
    private readonly CompiledFunction _function;

    public Frame(CompiledFunction function) => _function = function;

    public int InstructionPointer { get; set; } = -1;

    public ArraySegment<byte> GetInstructions() => _function.Instructions;
}
