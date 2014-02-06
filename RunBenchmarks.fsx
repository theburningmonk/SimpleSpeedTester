#I "examples/BinarySerializersBenchmark/bin/Release"
#I "examples/JsonSerializersBenchmark/bin/Release"
#I "examples/CollectionBenchmark/bin/Release"
#load "packages/FSharp.Charting.0.90.5/FSharp.Charting.fsx"

#r "SimpleSpeedTester.dll"
#r "BinarySerializersBenchmark.dll"
#r "JsonSerializersBenchmark.dll"
#r "CollectionBenchmark.dll"

open System
open System.Collections.Generic

open FSharp.Charting
open FSharp.Charting.ChartTypes

open SimpleSpeedTester.Example
open SimpleSpeedTester.Interfaces

let prettyPrint (results : Dictionary<string, ITestResultSummary * ITestResultSummary * double>) =
    let sortedResults = 
        results 
        |> Seq.sortBy (function 
            | KeyValue(_, (ser, null, _)) -> ser.AverageExecutionTime
            | KeyValue(_, (ser, deser, _)) -> ser.AverageExecutionTime + deser.AverageExecutionTime)

    printfn "-----------------------------------------------------------------------------------------------------------------------------"
    printfn "%-40s %-22s %-22s %-20s" "Name" "Serialization (ms)" "Deserialization (ms)" "Payload (bytes)"
    printfn "-----------------------------------------------------------------------------------------------------------------------------"

    for (KeyValue(name, (ser, deser, payload))) in sortedResults do
        printfn "%-40s %-22f %-22s %-20f" 
                name
                ser.AverageExecutionTime
                (match deser with | null -> "n/a" | x -> x.AverageExecutionTime.ToString())
                payload

    printfn "-----------------------------------------------------------------------------------------------------------------------------"

    let serResults   = sortedResults 
                       |> Seq.map (function (KeyValue(name, (ser, _, _))) -> name, ser.AverageExecutionTime)
    let deserResults = sortedResults 
                       |> Seq.map (function (KeyValue(name, (_, deser, _))) -> match deser with | null -> name, 0.0 | x -> name, x.AverageExecutionTime)
    
    Chart
        .Combine(
            [ Chart.Bar(serResults, Name = "Serialization", Color = Drawing.Color.Blue)
              Chart.Bar(deserResults, Name = "Deserialization", Color = Drawing.Color.Red) ])
        .WithXAxis(Enabled = true,
                   MajorGrid = Grid(Enabled = false),
                   MajorTickMark = TickMark(Enabled = true, Interval = 1.0, IntervalOffset = 1.0,
                                            Size = Windows.Forms.DataVisualization.Charting.TickMarkStyle.OutsideArea),
                   LabelStyle = LabelStyle.Create(TruncatedLabels = false, IsStaggered = false, 
                                                  Interval = 1.0))
        .WithYAxis(Enabled = true, Title = "Time (ms)",
                   MajorGrid  = Grid(Enabled = true),
                   MinorGrid  = Grid(Enabled = true, LineDashStyle = Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dot),
                   LabelStyle = LabelStyle.Create(TruncatedLabels = false, IsStaggered = false))
        .WithLegend(Enabled = true, Docking = Docking.Right, Alignment = Drawing.StringAlignment.Center)
        .ShowChart()

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

let runListBenchmark () =
    printfn "------- Lists --------"
    printfn "Running Benchmarks...\n\n\n\n\n"

    let results = CollectionSpeedTest.Run()

    printfn "all done."

let rec choose () =
    printfn "Choose benchmark to run:\n   1. Binary serializers\n   2. JSON serializers\n   3. Lists\n   4. Exit"
    let answer = Console.ReadLine()
    match answer with
    | "1" -> runBinaryBenchmark()
    | "2" -> runJsonBenchmark()
    | "3" -> runListBenchmark()
    | "4" -> printfn "bye!"
    | _ -> printfn "Sorry, I don't recognize that answer, please enter 1, 2, or 3"
           choose()

choose()