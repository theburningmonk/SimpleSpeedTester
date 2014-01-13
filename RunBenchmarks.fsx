#I "examples/BinarySerializersBenchmark/bin/Debug"
#I "examples/JsonSerializersBenchmark/bin/Debug"

#r "SimpleSpeedTester.dll"
#r "BinarySerializersBenchmark.dll"
#r "JsonSerializersBenchmark.dll"

open System
open System.Collections.Generic
open SimlpeSpeedTester.Example
open SimpleSpeedTester.Interfaces

let prettyPrint (results : Dictionary<string, ITestResultSummary * ITestResultSummary * double>) =
    let sortedResults = 
        results 
        |> Seq.sortBy (function 
            | KeyValue(_, (ser, null, _)) -> ser.AverageExecutionTime
            | KeyValue(_, (ser, deser, _)) -> ser.AverageExecutionTime + deser.AverageExecutionTime)

    printfn "----------------------------------------------------------------------------------------------------------------------------------"
    printfn "%-50s %-25s %-25s %-20s" "Name" "Serialization (ms)" "Deserialization (ms)" "Payload (bytes)"
    printfn "----------------------------------------------------------------------------------------------------------------------------------"

    for (KeyValue(name, (ser, deser, payload))) in sortedResults do
        printfn "%-50s %-25f %-25s %-20f" 
                name
                ser.AverageExecutionTime
                (match deser with | null -> "n/a" | x -> x.AverageExecutionTime.ToString())
                payload

    printfn "----------------------------------------------------------------------------------------------------------------------------------"

let runBinaryBenchmark () =
    printfn "------- Binary Serializers --------"
    printfn "Running Benchmarks...\n\n\n\n\n"

    let results = BinarySerializersSpeedTest.Run()

    printfn "all done."
    prettyPrint results

let runJsonBenchmark () =
    printfn "------- Json Serializers --------"
    printfn "Running Benchmarks...\n\n\n\n\n"

    let results = JsonSerializersSpeedTest.Run()

    printfn "all done."
    prettyPrint results

let rec choose () =
    printfn "Choose benchmark to run:\n   1. Binary serializers\n   2. JSON serializers\n   3. Exit"
    let answer = Console.ReadLine()
    match answer with
    | "1" -> runBinaryBenchmark()
             choose()
    | "2" -> runJsonBenchmark()
             choose()
    | "3" -> printfn "bye!"
    | _ -> printfn "Sorry, I don't recognize that answer, please enter 1, 2, or 3"
           choose()

choose()