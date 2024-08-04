using System;
using Monkey.Object;

namespace Monkey;

public class Frame
{
    private readonly CompiledFunction _function;

    public Frame(CompiledFunction function, int basePointer) => (_function, BasePointer) = (function, basePointer);

    public int InstructionPointer { get; set; } = -1;

    public int BasePointer { get; }

    public ArraySegment<byte> GetInstructions() => _function.Instructions;
}
