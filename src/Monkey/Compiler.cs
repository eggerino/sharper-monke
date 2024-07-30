using System.Collections.Generic;
using Monkey.Ast;
using Monkey.Object;

namespace Monkey;

public record ByteCode(IEnumerable<byte> Instructions, IReadOnlyList<IObject> Constants);

public class Compiler(INode node)
{
    public ByteCode? Compile()
    {
        return new([], []);
    }
}
