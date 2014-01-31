using System;
using System.Collections.Generic;

namespace SimlpeSpeedTester.Example
{
    public static class EnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var i in source)
            {
                action(i);
            }
        }
    }
}