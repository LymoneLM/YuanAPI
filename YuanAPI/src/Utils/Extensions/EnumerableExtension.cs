using System;
using System.Collections.Generic;

namespace YuanAPI;

public static class EnumerableExtension
{
    public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T, int> action)
    {
        var index = 0;
        foreach (var item in enumerable)
        {
            action(item, index);
            index++;
        }
    }
}
