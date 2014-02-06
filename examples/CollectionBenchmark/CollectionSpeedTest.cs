using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.FSharp.Collections;

using SimpleSpeedTester.Core;
using SimpleSpeedTester.Interfaces;

namespace SimpleSpeedTester.Example
{
    public class CollectionSpeedTest
    {
        // test serialization and deserialization on 100k objects
        private const int ObjectsCount = 100000;

        // perform 5 runs of each test
        private const int TestRuns = 1;

        public static Dictionary<string, Tuple<ITestResultSummary, ITestResultSummary>> Run()
        {
            var results = new Dictionary<string, Tuple<ITestResultSummary, ITestResultSummary>>();

            results.Add("List<T> with value type", AddItemsWithListT("List<T> with value type", 42));
            results.Add("List<T> with ref type", AddItemsWithListT("List<T> with ref type", "42"));

            results.Add("ImmutableList<T> with value type", AddItemsWithImmutableListT("ImmutableList<T> with value type", 42));
            results.Add("ImmutableList<T> with ref type", AddItemsWithImmutableListT("ImmutableList<T> with ref type", "42"));

            results.Add("FSharpList<T> with value type", AddItemsWithFSharpListT("FSharpList<T> with value type", 42));
            results.Add("FSharpList<T> with ref type", AddItemsWithFSharpListT("FSharpList<T> with ref type", "42"));

            return results;
        }

        private static Tuple<ITestResultSummary, ITestResultSummary> AddItemsWithListT<T>(string testGroupName, T item)
        {
            var list = new List<T>();

            var testGroup = new TestGroup(testGroupName);

            var addResultSummary =
                testGroup
                    .Plan("Add", () => Enumerable.Range(0, ObjectsCount).ForEach(_ => list.Add(item)), TestRuns)
                    .GetResult()
                    .GetSummary();

            Console.WriteLine(addResultSummary);

            var removeResultSummary =
                testGroup
                    .Plan("Remove From Head", () => Enumerable.Range(0, ObjectsCount).ForEach(_ => list.RemoveAt(0)), TestRuns)
                    .GetResult()
                    .GetSummary();

            Console.WriteLine(removeResultSummary);

            return Tuple.Create(addResultSummary, removeResultSummary);
        }

        private static Tuple<ITestResultSummary, ITestResultSummary> AddItemsWithImmutableListT<T>(string testGroupName, T item)
        {
            var list = ImmutableList<T>.Empty;

            var testGroup = new TestGroup(testGroupName);

            var addResultSummary =
                testGroup
                    .Plan("Add", () => Enumerable.Range(0, ObjectsCount).ForEach(_ => list = list.Add(item)), TestRuns)
                    .GetResult()
                    .GetSummary();

            Console.WriteLine(addResultSummary);

            var removeResultSummary =
                testGroup
                    .Plan("Remove From Head", () => Enumerable.Range(0, ObjectsCount).ForEach(_ => list = list.RemoveAt(0)), TestRuns)
                    .GetResult()
                    .GetSummary();

            Console.WriteLine(removeResultSummary);
            
            return Tuple.Create(addResultSummary, removeResultSummary);
        }

        private static Tuple<ITestResultSummary, ITestResultSummary> AddItemsWithFSharpListT<T>(string testGroupName, T item)
        {
            var list = FSharpList<T>.Empty;

            var testGroup = new TestGroup(testGroupName);

            var addResultSummary =
                testGroup
                    .Plan("Add", () => Enumerable.Range(0, ObjectsCount).ForEach(_ => list = FSharpList<T>.Cons(item, list)), TestRuns)
                    .GetResult()
                    .GetSummary();

            Console.WriteLine(addResultSummary);

            var removeResultSummary =
                testGroup
                    .Plan("Remove From Head", () => Enumerable.Range(0, ObjectsCount).ForEach(i => list = list.Tail), TestRuns)
                    .GetResult()
                    .GetSummary();

            Console.WriteLine(removeResultSummary);
            
            return Tuple.Create(addResultSummary, removeResultSummary);
        }
    }
}