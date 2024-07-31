using System;

namespace Monkey;

public static class ArrayExtensions
{
    public static ArraySegment<T> AsSegment<T>(this T[] array) => new ArraySegment<T>(array);
}
