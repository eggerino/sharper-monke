using Monkey.Object;

namespace Monkey;

public class Frame
{

    public Frame(Closure closure, int basePointer) => (Closure, BasePointer) = (closure, basePointer);

    public Closure Closure { get; }

    public int InstructionPointer { get; set; } = -1;

    public int BasePointer { get; }
}
