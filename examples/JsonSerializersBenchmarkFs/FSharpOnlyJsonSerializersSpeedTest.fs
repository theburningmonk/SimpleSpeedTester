namespace SimpleSpeedTester.Example

open SimpleSpeedTester.Core
open SimpleSpeedTester.Core.OutcomeFilters
open SimpleSpeedTester.Interfaces
open System
open System.Collections.Generic
open TestRecords

[<AutoOpen>]
module private Setup =
    let max  = 100000
    let runs = 5
    let rand = new Random(int DateTime.UtcNow.Ticks)
    let filter = ExcludeMinAndMaxTestOutcomeFilter()
    
    let createSimpleObj id =
        let scores =
            { 1..10 }
            |> Seq.map (fun _ -> rand.Next(1, 100))
            |> Seq.toArray

        {
            Id   = id
            Name = sprintf "Simple-%d" id
            Address = "Planet Earth"
            Scores  = scores
        }

    let simpleObjs = 
        { 1..max }
        |> Seq.map createSimpleObj
        |> Seq.toArray

module FSharpOnlyJsonSerializers =
    open Chiron

    let inline serializeWithChiron objs =
        objs |> Array.map (Json.serialize >> Json.format)

    let inline deserializeWithChiron jsons : SimpleRecord[] =
        jsons |> Array.map (Json.parse >> Json.deserialize)

    let benchmark 
            groupName 
            (serialize : 'a[] -> string[]) 
            (deserialize : string[] -> 'a[]) =
        let group = TestGroup(groupName)
        let mutable jsons = [||]
        let serPlan =
            group.Plan(
                "Serialization", 
                (fun () -> jsons <- serialize simpleObjs), 
                runs)
        let serSummary = serPlan.GetResult().GetSummary(filter)
        printfn "%O" serSummary

        let avgPayload = jsons |> Array.averageBy (Seq.length >> float)
        printfn 
            "Test Group [%s] average serialized byte array size is [%f]" 
            groupName 
            avgPayload

        let mutable objs = [||]
        let deserPlan =
            group.Plan(
                "Deserialization",
                (fun () -> objs <- deserialize jsons),
                runs)
        let deserSummary = deserPlan.GetResult().GetSummary(filter)
        printfn "%O" deserSummary

        printfn "---------------------------------------------------------\n\n"
        serSummary, deserSummary, avgPayload

    let run () =
        seq {
            yield "Chiron v6.0.1", benchmark "Chiron" serializeWithChiron deserializeWithChiron
        }
        |> fun results ->
            let dict = Dictionary<string, ITestResultSummary * ITestResultSummary * float>()
            for k, v in results do
                dict.[k] <- v

            dict