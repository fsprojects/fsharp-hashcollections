// Learn more about F# at http://fsharp.org
module Program 

open System

open System.Collections.Generic
open System.Diagnostics
open FSharp.HashCollections
open System.Collections.Concurrent
open BenchmarkDotNet
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open Persistent

module Constants = 
    let [<Literal>] OperationsPerInvokeInt = 100000

type ReadBenchmarks() = 

    let mutable sourceData = Array.zeroCreate 0
    let mutable hashMapData = HashMap.empty
    let mutable fsharpMapData = Map.empty
    let mutable keyToLookup = Array.zeroCreate Constants.OperationsPerInvokeInt
    let mutable dummyBuffer = Array.zeroCreate Constants.OperationsPerInvokeInt
    let randomGen = Random()

    [<Params(10, 100, 1000, 100_000, 500_000, 750_000, 1_000_000, 5_000_000, 10_000_000)>]
    member val public CollectionSize = 0 with get, set
    
    member this.SetupKeyToLookup() = 
        for i = 0 to keyToLookup.Length - 1 do
            keyToLookup.[i] <- randomGen.Next(0, this.CollectionSize)

    [<GlobalSetup(Target = "GetHashMap")>]
    member this.SetupHashMapData() = 
        hashMapData <- HashMap.empty        
        for i = 0 to this.CollectionSize - 1 do
            hashMapData <- hashMapData |> HashMap.add i i
        this.SetupKeyToLookup()

    [<GlobalSetup(Target = "GetFSharpMap")>]
    member this.SetupFSharpMapData() = 
        fsharpMapData <- Map.empty        
        for i = 0 to this.CollectionSize - 1 do
            fsharpMapData <- fsharpMapData |> Map.add i i
        this.SetupKeyToLookup()       

    [<Benchmark(OperationsPerInvoke = Constants.OperationsPerInvokeInt)>]
    member _.GetHashMap() = 
        let mutable i = 0
        for k in keyToLookup do
            dummyBuffer.[i] <- hashMapData |> HashMap.tryFind k |> ignore    
            i <- i + 1

    [<Benchmark(OperationsPerInvoke = Constants.OperationsPerInvokeInt)>]
    member _.GetFSharpMap() = 
        let mutable i = 0
        for k in keyToLookup do
            dummyBuffer.[i] <- fsharpMapData |> Map.tryFind k |> ignore
            i <- i + 1

[<EntryPoint>]
let main argv =
    let summary = BenchmarkRunner.Run(typeof<ReadBenchmarks>.Assembly);
    //printfn "%A" summary
    0

