module Program 

open System

open System.Collections.Generic
open System.Diagnostics
open FSharp.HashCollections
open System.Collections.Concurrent
open BenchmarkDotNet
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open FSharpx.Collections

module Constants = 
    let [<Literal>] OperationsPerInvokeInt = 100000

type ReadBenchmarks() = 

    let mutable hashMapData = HashMap.empty
    let mutable fsharpMapData = Map.empty
    let mutable thirdPartyMapData = Persistent.PersistentHashMap.empty
    let mutable fsharpXHashMap = FSharpx.Collections.PersistentHashMap.empty
    let mutable systemImmutableMap = System.Collections.Immutable.ImmutableDictionary.Empty
    let mutable keyToLookup = Array.zeroCreate Constants.OperationsPerInvokeInt
    let mutable dummyBufferVOption = Array.zeroCreate Constants.OperationsPerInvokeInt
    let mutable dummyBufferOption = Array.zeroCreate Constants.OperationsPerInvokeInt
    let mutable dummyBufferNoOption = Array.zeroCreate Constants.OperationsPerInvokeInt
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

    [<GlobalSetup(Target = "GetThirdPartyMap")>]
    member this.SetupThirdPartyMapData() = 
        thirdPartyMapData <- Persistent.PersistentHashMap.empty        
        for i = 0 to this.CollectionSize - 1 do
            thirdPartyMapData <- thirdPartyMapData |> Persistent.PersistentHashMap.set i i
        this.SetupKeyToLookup()       

    [<GlobalSetup(Target = "GetFSharpXHashMap")>]
    member this.SetupFSharpXMapData() = 
        fsharpXHashMap <- FSharpx.Collections.PersistentHashMap.empty        
        for i = 0 to this.CollectionSize - 1 do
            fsharpXHashMap <- fsharpXHashMap |> FSharpx.Collections.PersistentHashMap.add i i
        this.SetupKeyToLookup() 

    [<GlobalSetup(Target = "GetSystemCollectionsImmutableMap")>]
    member this.SetupSystemCollectionsImmutableMapData() = 
        systemImmutableMap <- System.Collections.Immutable.ImmutableDictionary.Empty 
        for i = 0 to this.CollectionSize - 1 do
            systemImmutableMap <- systemImmutableMap.Add(i, i)
        this.SetupKeyToLookup()  

    [<Benchmark(OperationsPerInvoke = Constants.OperationsPerInvokeInt)>]
    member _.GetHashMap() = 
        let mutable i = 0
        for k in keyToLookup do
            dummyBufferVOption.[i] <- hashMapData |> HashMap.tryFind k
            i <- i + 1

    // [<Benchmark(OperationsPerInvoke = Constants.OperationsPerInvokeInt)>]
    // member _.GetFSharpMap() = 
    //     let mutable i = 0
    //     for k in keyToLookup do
    //         dummyBufferOption.[i] <- fsharpMapData |> Map.tryFind k
    //         i <- i + 1

    [<Benchmark(OperationsPerInvoke = Constants.OperationsPerInvokeInt)>]
    member _.GetThirdPartyMap() = 
        let mutable i = 0
        for k in keyToLookup do
            dummyBufferOption.[i] <- thirdPartyMapData |> Persistent.PersistentHashMap.tryFind k
            i <- i + 1

    // [<Benchmark(OperationsPerInvoke = Constants.OperationsPerInvokeInt)>]
    // member _.GetFSharpXHashMap() = 
    //     let mutable i = 0
    //     for k in keyToLookup do
    //         dummyBufferNoOption.[i] <- fsharpXHashMap |> FSharpx.Collections.PersistentHashMap.find k
    //         i <- i + 1

    // [<Benchmark(OperationsPerInvoke = Constants.OperationsPerInvokeInt)>]
    // member _.GetSystemCollectionsImmutableMap() = 
    //     let mutable i = 0
    //     for k in keyToLookup do
    //         match systemImmutableMap.TryGetValue(k) with
    //         | (true, x) -> dummyBufferNoOption.[i] <- x
    //         | _ -> ()
    //         i <- i + 1            

[<EntryPoint>]
let main argv =
    let summary = BenchmarkRunner.Run(typeof<ReadBenchmarks>.Assembly);
    //printfn "%A" summary
    0

