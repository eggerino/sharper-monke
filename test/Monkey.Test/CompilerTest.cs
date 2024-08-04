using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using Monkey.Ast;
using Monkey.Code;
using Monkey.Object;

namespace Monkey.Test;

public class CompilerTest
{
    record CompilerTestCase(
        string Input,
        IReadOnlyList<object> ExpectedConstants,
        IEnumerable<IEnumerable<byte>> ExpectedInstructions);

    [Fact]
    public void TestIntegerArithmetic()
    {
        RunCompilerTests([
            new(Input: "1 + 2",
                ExpectedConstants: [1, 2],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.Constant, 1),
                    Instruction.Make(Opcode.Add),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "1; 2",
                ExpectedConstants: [1, 2],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.Pop),
                    Instruction.Make(Opcode.Constant, 1),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "1 - 2",
                ExpectedConstants: [1, 2],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.Constant, 1),
                    Instruction.Make(Opcode.Sub),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "1 * 2",
                ExpectedConstants: [1, 2],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.Constant, 1),
                    Instruction.Make(Opcode.Mul),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "2 / 1",
                ExpectedConstants: [2, 1],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.Constant, 1),
                    Instruction.Make(Opcode.Div),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "-1",
                ExpectedConstants: [1],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.Minus),
                    Instruction.Make(Opcode.Pop),
                ]),
        ]);
    }

    [Fact]
    public void TestBooleanExpressions()
    {
        RunCompilerTests([
            new(Input: "true",
                ExpectedConstants: [],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.True),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "false",
                ExpectedConstants: [],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.False),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "1 > 2",
                ExpectedConstants: [1, 2],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.Constant, 1),
                    Instruction.Make(Opcode.GreaterThan),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "1 < 2",
                ExpectedConstants: [2, 1],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.Constant, 1),
                    Instruction.Make(Opcode.GreaterThan),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "1 == 2",
                ExpectedConstants: [1, 2],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.Constant, 1),
                    Instruction.Make(Opcode.Equal),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "1 != 2",
                ExpectedConstants: [1, 2],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.Constant, 1),
                    Instruction.Make(Opcode.NotEqual),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "true == false",
                ExpectedConstants: [],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.True),
                    Instruction.Make(Opcode.False),
                    Instruction.Make(Opcode.Equal),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "true != false",
                ExpectedConstants: [],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.True),
                    Instruction.Make(Opcode.False),
                    Instruction.Make(Opcode.NotEqual),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "!true",
                ExpectedConstants: [],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.True),
                    Instruction.Make(Opcode.Bang),
                    Instruction.Make(Opcode.Pop),
                ]),
        ]);
    }

    [Fact]
    public void TestConditionals()
    {
        RunCompilerTests([
            new(Input: "if (true) { 10 }; 3333;",
                ExpectedConstants: [10, 3333],
                ExpectedInstructions:[
                    Instruction.Make(Opcode.True),                  // 0000
                    Instruction.Make(Opcode.JumpNotTruthy, 10),     // 0001
                    Instruction.Make(Opcode.Constant, 0),           // 0004
                    Instruction.Make(Opcode.Jump, 11),              // 0007
                    Instruction.Make(Opcode.Null),                  // 0010
                    Instruction.Make(Opcode.Pop),                   // 0011
                    Instruction.Make(Opcode.Constant, 1),           // 0012
                    Instruction.Make(Opcode.Pop),                   // 0015
                ]),
            new(Input: "if (true) { 10 } else { 20 }; 3333;",
                ExpectedConstants: [10, 20, 3333],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.True),                  // 0000
                    Instruction.Make(Opcode.JumpNotTruthy, 10),     // 0001
                    Instruction.Make(Opcode.Constant, 0),           // 0004
                    Instruction.Make(Opcode.Jump, 13),              // 0007
                    Instruction.Make(Opcode.Constant, 1),           // 0010
                    Instruction.Make(Opcode.Pop),                   // 0013
                    Instruction.Make(Opcode.Constant, 2),           // 0014
                    Instruction.Make(Opcode.Pop),                   // 0017
                ]),
        ]);
    }

    [Fact]
    public void TestGlobalLetStatements()
    {
        RunCompilerTests([
            new(Input: "let one = 1; let two = 2;",
                ExpectedConstants: [1, 2],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.SetGlobal, 0),
                    Instruction.Make(Opcode.Constant, 1),
                    Instruction.Make(Opcode.SetGlobal, 1),
                ]),
            new(Input: "let one = 1; one;",
                ExpectedConstants: [1],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.SetGlobal, 0),
                    Instruction.Make(Opcode.GetGlobal, 0),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "let one = 1; let two = one; two;",
                ExpectedConstants: [1],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.SetGlobal, 0),
                    Instruction.Make(Opcode.GetGlobal, 0),
                    Instruction.Make(Opcode.SetGlobal, 1),
                    Instruction.Make(Opcode.GetGlobal, 1),
                    Instruction.Make(Opcode.Pop),
                ]),
        ]);
    }

    [Fact]
    public void TestStringExpressions()
    {
        RunCompilerTests([
            new(Input: @"""monkey""",
                ExpectedConstants: ["monkey"],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: @"""mon"" + ""key""",
                ExpectedConstants: ["mon", "key"],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.Constant, 1),
                    Instruction.Make(Opcode.Add),
                    Instruction.Make(Opcode.Pop),
                ]),
        ]);
    }

    [Fact]
    public void TestArrayLiterals()
    {
        RunCompilerTests([
            new(Input: "[]",
                ExpectedConstants: [],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Array, 0),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "[1, 2, 3]",
                ExpectedConstants: [1, 2, 3],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.Constant, 1),
                    Instruction.Make(Opcode.Constant, 2),
                    Instruction.Make(Opcode.Array, 3),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "[1 + 2, 3 - 4, 5 * 6]",
                ExpectedConstants: [1, 2, 3, 4, 5, 6],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.Constant, 1),
                    Instruction.Make(Opcode.Add),
                    Instruction.Make(Opcode.Constant, 2),
                    Instruction.Make(Opcode.Constant, 3),
                    Instruction.Make(Opcode.Sub),
                    Instruction.Make(Opcode.Constant, 4),
                    Instruction.Make(Opcode.Constant, 5),
                    Instruction.Make(Opcode.Mul),
                    Instruction.Make(Opcode.Array, 3),
                    Instruction.Make(Opcode.Pop),
                ]),
        ]);
    }

    [Fact]
    public void TestHashLiterals()
    {
        RunCompilerTests([
            new (Input: "{}",
                ExpectedConstants: [],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Hash, 0),
                    Instruction.Make(Opcode.Pop),
                ]),
            new (Input: "{1: 2, 3: 4, 5: 6}",
                ExpectedConstants: [1, 2, 3, 4, 5, 6],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.Constant, 1),
                    Instruction.Make(Opcode.Constant, 2),
                    Instruction.Make(Opcode.Constant, 3),
                    Instruction.Make(Opcode.Constant, 4),
                    Instruction.Make(Opcode.Constant, 5),
                    Instruction.Make(Opcode.Hash, 6),
                    Instruction.Make(Opcode.Pop),
                ]),
            new (Input: "{1: 2 + 3, 4: 5 * 6}",
                ExpectedConstants: [1, 2, 3, 4, 5, 6],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.Constant, 1),
                    Instruction.Make(Opcode.Constant, 2),
                    Instruction.Make(Opcode.Add),
                    Instruction.Make(Opcode.Constant, 3),
                    Instruction.Make(Opcode.Constant, 4),
                    Instruction.Make(Opcode.Constant, 5),
                    Instruction.Make(Opcode.Mul),
                    Instruction.Make(Opcode.Hash, 4),
                    Instruction.Make(Opcode.Pop),
                ]),
        ]);
    }

    [Fact]
    public void TestIndexExpressions()
    {
        RunCompilerTests([
            new(Input: "[1, 2, 3][1 + 1]",
                ExpectedConstants: [1, 2, 3, 1, 1],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.Constant, 1),
                    Instruction.Make(Opcode.Constant, 2),
                    Instruction.Make(Opcode.Array, 3),
                    Instruction.Make(Opcode.Constant, 3),
                    Instruction.Make(Opcode.Constant, 4),
                    Instruction.Make(Opcode.Add),
                    Instruction.Make(Opcode.Index),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "{1: 2}[2 - 1]",
                ExpectedConstants: [1, 2, 2, 1],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.Constant, 1),
                    Instruction.Make(Opcode.Hash, 2),
                    Instruction.Make(Opcode.Constant, 2),
                    Instruction.Make(Opcode.Constant, 3),
                    Instruction.Make(Opcode.Sub),
                    Instruction.Make(Opcode.Index),
                    Instruction.Make(Opcode.Pop),
                ]),
        ]);
    }

    [Fact]
    public void TestFunctions()
    {
        RunCompilerTests([
            new(Input: "fn() { return 5 + 10 }",
                ExpectedConstants: [
                    5,
                    10,
                    new IEnumerable<byte>[]
                    {
                        Instruction.Make(Opcode.Constant, 0),
                        Instruction.Make(Opcode.Constant, 1),
                        Instruction.Make(Opcode.Add),
                        Instruction.Make(Opcode.ReturnValue),
                    },
                ],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Closure, 2, 0),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "fn() { 5 + 10 }",
                ExpectedConstants: [
                    5,
                    10,
                    new IEnumerable<byte>[]
                    {
                        Instruction.Make(Opcode.Constant, 0),
                        Instruction.Make(Opcode.Constant, 1),
                        Instruction.Make(Opcode.Add),
                        Instruction.Make(Opcode.ReturnValue),
                    },
                ],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Closure, 2, 0),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "fn() { 1; 2 }",
                ExpectedConstants: [
                    1,
                    2,
                    new IEnumerable<byte>[]
                    {
                        Instruction.Make(Opcode.Constant, 0),
                        Instruction.Make(Opcode.Pop),
                        Instruction.Make(Opcode.Constant, 1),
                        Instruction.Make(Opcode.ReturnValue),
                    },
                ],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Closure, 2, 0),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "fn() { }",
                ExpectedConstants: [
                    new IEnumerable<byte>[]
                    {
                        Instruction.Make(Opcode.Return),
                    },
                ],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Closure, 0, 0),
                    Instruction.Make(Opcode.Pop),
                ]),
        ]);
    }

    [Fact]
    public void TestFunctionCalls()
    {
        RunCompilerTests([
            new(Input: "fn() { 24 }();",
                ExpectedConstants: [
                    24,
                    new IEnumerable<byte>[]
                    {
                        Instruction.Make(Opcode.Constant, 0),
                        Instruction.Make(Opcode.ReturnValue),
                    }
                ],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Closure, 1, 0),
                    Instruction.Make(Opcode.Call, 0),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "let noArg = fn() { 24 }; noArg();",
                ExpectedConstants: [
                    24,
                    new IEnumerable<byte>[]
                    {
                        Instruction.Make(Opcode.Constant, 0),
                        Instruction.Make(Opcode.ReturnValue),
                    }
                ],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Closure, 1, 0),
                    Instruction.Make(Opcode.SetGlobal, 0),
                    Instruction.Make(Opcode.GetGlobal, 0),
                    Instruction.Make(Opcode.Call, 0),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "let oneArg = fn(a) { a }; oneArg(24);",
                ExpectedConstants: [
                    new IEnumerable<byte>[]
                    {
                        Instruction.Make(Opcode.GetLocal, 0),
                        Instruction.Make(Opcode.ReturnValue),
                    },
                    24,
                ],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Closure, 0, 0),
                    Instruction.Make(Opcode.SetGlobal, 0),
                    Instruction.Make(Opcode.GetGlobal, 0),
                    Instruction.Make(Opcode.Constant, 1),
                    Instruction.Make(Opcode.Call, 1),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "let manyArg = fn(a, b, c) { a; b; c }; manyArg(24, 25, 26);",
                ExpectedConstants: [
                    new IEnumerable<byte>[]
                    {
                        Instruction.Make(Opcode.GetLocal, 0),
                        Instruction.Make(Opcode.Pop),
                        Instruction.Make(Opcode.GetLocal, 1),
                        Instruction.Make(Opcode.Pop),
                        Instruction.Make(Opcode.GetLocal, 2),
                        Instruction.Make(Opcode.ReturnValue),
                    },
                    24,
                    25,
                    26,
                ],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Closure, 0, 0),
                    Instruction.Make(Opcode.SetGlobal, 0),
                    Instruction.Make(Opcode.GetGlobal, 0),
                    Instruction.Make(Opcode.Constant, 1),
                    Instruction.Make(Opcode.Constant, 2),
                    Instruction.Make(Opcode.Constant, 3),
                    Instruction.Make(Opcode.Call, 3),
                    Instruction.Make(Opcode.Pop),
                ]),
        ]);
    }

    [Fact]
    public void TestLetStatementScopes()
    {
        RunCompilerTests([
            new(Input: "let num = 55; fn() { num }",
                ExpectedConstants: [
                    55,
                    new IEnumerable<byte>[]
                    {
                        Instruction.Make(Opcode.GetGlobal, 0),
                        Instruction.Make(Opcode.ReturnValue),
                    },
                ],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.SetGlobal, 0),
                    Instruction.Make(Opcode.Closure, 1, 0),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "let num = 55; num",
                ExpectedConstants: [
                    55,
                ],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.SetGlobal, 0),
                    Instruction.Make(Opcode.GetGlobal, 0),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "fn() { let a = 55; let b = 77; a + b }",
                ExpectedConstants: [
                    55,
                    77,
                    new IEnumerable<byte>[]
                    {
                        Instruction.Make(Opcode.Constant, 0),
                        Instruction.Make(Opcode.SetLocal, 0),
                        Instruction.Make(Opcode.Constant, 1),
                        Instruction.Make(Opcode.SetLocal, 1),
                        Instruction.Make(Opcode.GetLocal, 0),
                        Instruction.Make(Opcode.GetLocal, 1),
                        Instruction.Make(Opcode.Add),
                        Instruction.Make(Opcode.ReturnValue),
                    },
                ],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Closure, 2, 0),
                    Instruction.Make(Opcode.Pop),
                ]),
        ]);
    }

    [Fact]
    public void TestBuiltins()
    {
        RunCompilerTests([
            new(Input: "len([]); push([], 1);",
                ExpectedConstants: [1],
                ExpectedInstructions:[
                    Instruction.Make(Opcode.GetBuiltin, 0),
                    Instruction.Make(Opcode.Array, 0),
                    Instruction.Make(Opcode.Call, 1),
                    Instruction.Make(Opcode.Pop),
                    Instruction.Make(Opcode.GetBuiltin, 4),
                    Instruction.Make(Opcode.Array, 0),
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.Call, 2),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: "fn() { len([]) }",
                ExpectedConstants: [
                    new IEnumerable<byte>[]
                    {
                        Instruction.Make(Opcode.GetBuiltin, 0),
                        Instruction.Make(Opcode.Array, 0),
                        Instruction.Make(Opcode.Call, 1),
                        Instruction.Make(Opcode.ReturnValue),
                    }
                ],
                ExpectedInstructions:[
                    Instruction.Make(Opcode.Closure, 0, 0),
                    Instruction.Make(Opcode.Pop),
                ]),
        ]);
    }

    [Fact]
    public void TestClosure()
    {
        RunCompilerTests([
            new(Input: """
                fn(a) {
                    fn(b) {
                        a + b
                    }
                }
                """,
                ExpectedConstants: [
                    new IEnumerable<byte>[]
                    {
                        Instruction.Make(Opcode.GetFree, 0),
                        Instruction.Make(Opcode.GetLocal, 0),
                        Instruction.Make(Opcode.Add),
                        Instruction.Make(Opcode.ReturnValue),
                    },
                    new IEnumerable<byte>[]
                    {
                        Instruction.Make(Opcode.GetLocal, 0),
                        Instruction.Make(Opcode.Closure, 0, 1),
                        Instruction.Make(Opcode.ReturnValue),
                    },
                ], ExpectedInstructions: [
                    Instruction.Make(Opcode.Closure, 1, 0),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: """
                fn(a) {
                    fn(b) {
                        fn(c) {
                            a + b + c
                        }
                    }
                };
                """,
                ExpectedConstants: [
                    new IEnumerable<byte>[]
                    {
                        Instruction.Make(Opcode.GetFree, 0),
                        Instruction.Make(Opcode.GetFree, 1),
                        Instruction.Make(Opcode.Add),
                        Instruction.Make(Opcode.GetLocal, 0),
                        Instruction.Make(Opcode.Add),
                        Instruction.Make(Opcode.ReturnValue),
                    },
                    new IEnumerable<byte>[]
                    {
                        Instruction.Make(Opcode.GetFree, 0),
                        Instruction.Make(Opcode.GetLocal, 0),
                        Instruction.Make(Opcode.Closure, 0, 2),
                        Instruction.Make(Opcode.ReturnValue),
                    },
                    new IEnumerable<byte>[]
                    {
                        Instruction.Make(Opcode.GetLocal, 0),
                        Instruction.Make(Opcode.Closure, 1, 1),
                        Instruction.Make(Opcode.ReturnValue),
                    },
                ],
                ExpectedInstructions: [
                        Instruction.Make(Opcode.Closure, 2, 0),
                        Instruction.Make(Opcode.Pop),
                ]),
            new(Input: """
                let global = 55;

                fn() {
                    let a = 66;

                    fn() {
                        let b = 77;

                        fn() {
                            let c = 88;

                            global + a + b + c;
                        }
                    }
                }
                """,
                ExpectedConstants: [
                    55, 66, 77, 88,
                    new IEnumerable<byte>[]
                    {
                        Instruction.Make(Opcode.Constant, 3),
                        Instruction.Make(Opcode.SetLocal, 0),
                        Instruction.Make(Opcode.GetGlobal, 0),
                        Instruction.Make(Opcode.GetFree, 0),
                        Instruction.Make(Opcode.Add),
                        Instruction.Make(Opcode.GetFree, 1),
                        Instruction.Make(Opcode.Add),
                        Instruction.Make(Opcode.GetLocal, 0),
                        Instruction.Make(Opcode.Add),
                        Instruction.Make(Opcode.ReturnValue),
                    },
                    new IEnumerable<byte>[]
                    {
                        Instruction.Make(Opcode.Constant, 2),
                        Instruction.Make(Opcode.SetLocal, 0),
                        Instruction.Make(Opcode.GetFree, 0),
                        Instruction.Make(Opcode.GetLocal, 0),
                        Instruction.Make(Opcode.Closure, 4, 2),
                        Instruction.Make(Opcode.ReturnValue),
                    },
                    new IEnumerable<byte>[]
                    {
                        Instruction.Make(Opcode.Constant, 1),
                        Instruction.Make(Opcode.SetLocal, 0),
                        Instruction.Make(Opcode.GetLocal, 0),
                        Instruction.Make(Opcode.Closure, 5, 1),
                        Instruction.Make(Opcode.ReturnValue),
                    },
                ],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Constant, 0),
                    Instruction.Make(Opcode.SetGlobal, 0),
                    Instruction.Make(Opcode.Closure, 6, 0),
                    Instruction.Make(Opcode.Pop),
                ]),
        ]);
    }

    [Fact]
    public void TestRecursiveFunction()
    {
        RunCompilerTests([
            new(Input: """
                let countDown = fn(x) { countDown(x - 1); };
                countDown(1);
                """,
                ExpectedConstants: [
                    1,
                    new IEnumerable<byte>[]
                    {
                        Instruction.Make(Opcode.CurrentClosure),
                        Instruction.Make(Opcode.GetLocal, 0),
                        Instruction.Make(Opcode.Constant, 0),
                        Instruction.Make(Opcode.Sub),
                        Instruction.Make(Opcode.Call, 1),
                        Instruction.Make(Opcode.ReturnValue),
                    },
                    1,
                ],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Closure, 1, 0),
                    Instruction.Make(Opcode.SetGlobal, 0),
                    Instruction.Make(Opcode.GetGlobal, 0),
                    Instruction.Make(Opcode.Constant, 2),
                    Instruction.Make(Opcode.Call, 1),
                    Instruction.Make(Opcode.Pop),
                ]),
            new(Input: """
                let wrapper = fn() {
                    let countDown = fn(x) { countDown(x - 1); };
                    countDown(1);
                };
                wrapper();
                """,
                ExpectedConstants: [
                    1,
                    new IEnumerable<byte>[]
                    {
                        Instruction.Make(Opcode.CurrentClosure),
                        Instruction.Make(Opcode.GetLocal, 0),
                        Instruction.Make(Opcode.Constant, 0),
                        Instruction.Make(Opcode.Sub),
                        Instruction.Make(Opcode.Call, 1),
                        Instruction.Make(Opcode.ReturnValue),
                    },
                    1,
                    new IEnumerable<byte>[]
                    {
                        Instruction.Make(Opcode.Closure, 1, 0),
                        Instruction.Make(Opcode.SetLocal, 0),
                        Instruction.Make(Opcode.GetLocal, 0),
                        Instruction.Make(Opcode.Constant, 2),
                        Instruction.Make(Opcode.Call, 1),
                        Instruction.Make(Opcode.ReturnValue),
                    },
                ],
                ExpectedInstructions: [
                    Instruction.Make(Opcode.Closure, 3, 0),
                    Instruction.Make(Opcode.SetGlobal, 0),
                    Instruction.Make(Opcode.GetGlobal, 0),
                    Instruction.Make(Opcode.Call, 0),
                    Instruction.Make(Opcode.Pop),
                ]),
        ]);
    }

    private static void RunCompilerTests(IEnumerable<CompilerTestCase> tests)
    {
        foreach (var test in tests)
        {
            var program = Parse(test.Input);
            var compiler = new Compiler();

            var error = compiler.Compile(program);
            Assert.Null(error);

            var bytecode = compiler.GetByteCode();

            Assert.NotNull(bytecode);
            TestInstructions(test.ExpectedInstructions, bytecode.Instructions);
            TestConstants(test.ExpectedConstants, bytecode.Constants);
        }
    }

    private static Program Parse(string input)
    {
        var lexer = new Lexer(input);
        var parser = new Parser(lexer);
        var (program, _) = parser.ParseProgram();
        return program;
    }

    private static void TestInstructions(IEnumerable<IEnumerable<byte>> expected, IEnumerable<byte> actual)
    {
        var expectedAssembly = expected.SelectMany(x => x).ToArray().AsSegment().Disassemble();
        var actualAssembly = actual.ToArray().AsSegment().Disassemble();

        Assert.Equal(expectedAssembly, actualAssembly);
    }

    private static void TestConstants(IReadOnlyCollection<object> expected, IReadOnlyCollection<IObject> actual)
    {
        Assert.Equal(expected.Count, actual.Count);

        foreach (var (exp, act) in expected.Zip(actual))
        {
            if (exp is int expInt)
            {
                TestIntegerObject(expInt, act);
            }
            else if (exp is string expStr)
            {
                TestStringObject(expStr, act);
            }
            else if (exp is IEnumerable<byte>[] expCode)
            {
                TestCompiledFunctionObject(expCode, act);
            }
            else
            {
                Assert.Fail("Unexpected variant");
            }
        }
    }

    private static void TestIntegerObject(long expected, IObject actual)
    {
        var actualInt = Assert.IsType<Integer>(actual);
        Assert.Equal(expected, actualInt.Value);
    }

    private static void TestStringObject(string expected, IObject actual)
    {
        var actualStr = Assert.IsType<Object.String>(actual);
        Assert.Equal(expected, actualStr.Value);
    }

    private static void TestCompiledFunctionObject(IEnumerable<byte>[] expected, IObject actual)
    {
        var actualFunc = Assert.IsType<CompiledFunction>(actual);
        TestInstructions(expected, actualFunc.Instructions);
    }
}
