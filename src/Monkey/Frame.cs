using System;
using Monkey.Object;

namespace Monkey;

public class Frame
{
    private readonly Closure _closure;

    public Frame(Closure closure, int basePointer) => (_closure, BasePointer) = (closure, basePointer);

    public int InstructionPointer { get; set; } = -1;

    public int BasePointer { get; }

    public ArraySegment<byte> GetInstructions() => _closure.Function.Instructions;
}
